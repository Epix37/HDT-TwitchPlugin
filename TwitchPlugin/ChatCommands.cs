﻿#region

using System;
using System.Linq;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Stats;

#endregion

namespace TwitchPlugin
{
	public class ChatCommands
	{
		private const string HssUrl = "http://hss.io/d/";
		private static int _winStreak;
		private static GameStats _lastGame;
		private static readonly string[] KillingSprees = {"Killing Spree", "Rampage", "Dominating", "Unstoppable", "GODLIKE", "WICKED SICK"};

		public static void AllDecksCommand()
		{
			var decks = DeckList.Instance.Decks.Where(d => d.Tags.Contains(Core.TwitchTag)).ToList();
			if(!decks.Any())
				return;
			var response = decks.Select(d => string.Format("{0}: {1}", d.Name, HssUrl + d.HearthStatsId)).Aggregate((c, n) => c + ", " + n);
			Core.Send(response);
		}

		public static void DeckCommand()
		{
			var deck = DeckList.Instance.ActiveDeck;
			if(deck.IsArenaDeck)
			{
				Core.Send(string.Format("Current arena run ({0}): {1}, DeckList: {2}", deck.Class, deck.WinLossString,
				                        "[currently only supported for constructed decks]"));
			}
			else
			{
				Core.Send(string.Format("Currently using \"{0}\", Winrate: {1} ({2}), Decklist: {3}", deck.Name, deck.WinPercentString,
				                        deck.WinLossString, HssUrl + deck.HearthStatsId));
			}
		}

		public static void StatsCommand(string arg)
		{
			var games = DeckStatsList.Instance.DeckStats.SelectMany(ds => ds.Games).Where(TimeFrameFilter(arg)).ToList();
			var numGames = games.Count;
			var numDecks = games.Select(g => g.DeckId).Distinct().Count();
			var wins = games.Count(g => g.Result == GameResult.Win);
			var winRate = Math.Round(100.0 * wins / numGames);
			Core.Send(string.Format("Played {0} games with {1} decks. Total stats: {2}-{3} ({4}%)", numGames, numDecks, wins, numGames - wins,
			                        winRate));
		}

		public static void ArenaCommand(string arg)
		{
			var arenaRuns = DeckList.Instance.Decks.Where(d => d.IsArenaDeck).ToList();
			switch(arg)
			{
				case "today":
					arenaRuns = arenaRuns.Where(g => g.LastPlayed.Date == DateTime.Today).ToList();
					break;
				case "week":
					arenaRuns = arenaRuns.Where(g => g.LastPlayed.Date > DateTime.Today.AddDays(-7)).ToList();
					break;
				case "season":
					arenaRuns =
						arenaRuns.Where(g => g.LastPlayed.Date.Year == DateTime.Today.Year && g.LastPlayed.Date.Month == DateTime.Today.Month).ToList();
					break;
				case "total":
					break;
				default:
					return;
			}
			var timeFrame = arg == "today" ? arg : "this " + arg;
			if(!arenaRuns.Any())
			{
				Core.Send(string.Format("No arena runs {0}.", timeFrame));
				return;
			}
			var ordered =
				arenaRuns.Select(run => new {Run = run, Wins = run.DeckStats.Games.Count(g => g.Result == GameResult.Win)})
				         .OrderByDescending(x => x.Wins);
			var best = ordered.First();
			var count = ordered.Count(x => x.Wins == best.Wins);
			var countString = count > 1 ? string.Format(" ({0} times)", count) : "";
			Core.Send(string.Format("Best arena run {0}: {1} with {2}{3}", timeFrame, best.Run.WinLossString, best.Run.Class, countString));
		}

		public static void BestDeckCommand(string arg)
		{
			var decks =
				DeckList.Instance.Decks.Where(d => !d.IsArenaDeck)
				        .Select(d => new {Deck = d, Games = d.DeckStats.Games.Where(TimeFrameFilter(arg))});
			var stats =
				decks.Select(
				             d =>
				             new
				             {
					             DeckObj = d,
					             Wins = d.Games.Count(g => g.Result == GameResult.Win),
					             Losses = (d.Games.Count(g => g.Result == GameResult.Loss))
				             })
				     .Where(d => d.Wins + d.Losses > Config.Instance.BestDeckGamesThreshold)
				     .OrderByDescending(d => (double)d.Wins / (d.Wins + d.Losses));
			var best = stats.FirstOrDefault();
			var timeFrame = arg == "today" || arg == "total" ? arg : "this " + arg;
			if(best == null)
			{
				if(Config.Instance.BestDeckGamesThreshold > 1)
					Core.Send(string.Format("Not enough games played {0} (min: {1})", timeFrame, Config.Instance.BestDeckGamesThreshold));
				else
					Core.Send("No games played " + timeFrame);
				return;
			}
			var winRate = Math.Round(100.0 * best.Wins / (best.Wins + best.Losses), 0);
			Core.Send(string.Format("Best deck {0}: \"{1}\", Winrate: {2}% ({3}-{4}), Decklist: {5}", timeFrame, best.DeckObj.Deck.Name, winRate,
			                        best.Wins, best.Losses, HssUrl + best.DeckObj.Deck.HearthStatsId));
		}

		public static void MostPlayedCommand(string arg)
		{
			var decks =
				DeckList.Instance.Decks.Where(d => !d.IsArenaDeck)
				        .Select(d => new {Deck = d, Games = d.DeckStats.Games.Where(TimeFrameFilter(arg))});
			var mostPlayed = decks.Where(d => d.Games.Any()).OrderByDescending(d => d.Games.Count()).FirstOrDefault();
			var timeFrame = arg == "today" || arg == "total" ? arg : "this " + arg;
			if(mostPlayed == null)
			{
				Core.Send("No games played " + timeFrame);
				return;
			}
			var wins = mostPlayed.Games.Count(g => g.Result == GameResult.Win);
			var losses = mostPlayed.Games.Count(g => g.Result == GameResult.Loss);
			var winRate = Math.Round(100.0 * wins / (wins + losses), 0);
			Core.Send(string.Format("Most played deck {0}: \"{1}\", Winrate: {2}% ({3}-{4}), Decklist: {5}", timeFrame, mostPlayed.Deck.Name,
			                        winRate, wins, losses, HssUrl + mostPlayed.Deck.HearthStatsId));
		}

		public static Func<GameStats, bool> TimeFrameFilter(string timeFrame)
		{
			switch(timeFrame)
			{
				case "today":
					return game => game.StartTime == DateTime.Today;
				case "week":
					return game => game.StartTime > DateTime.Today.AddDays(-7);
				case "season":
					return game => game.StartTime.Date.Year == DateTime.Today.Year && game.StartTime.Date.Month == DateTime.Today.Month;
				case "total":
					return game => true;
				default:
					return game => false;
			}
		}

		public static void HdtCommand()
		{
			Core.Send(string.Format("Hearthstone Deck Tracker: https://github.com/Epix37/Hearthstone-Deck-Tracker/releases"));
		}

		public static void OnGameEnd()
		{
			_lastGame = Game.CurrentGameStats.CloneWithNewId();
			if(_lastGame.Result == GameResult.Win)
				_winStreak++;
			else
				_winStreak = 0;
		}

		public static void OnInMenu()
		{
			if(!Config.Instance.AutoPostGameResult)
				return;
			if(_lastGame == null)
				return;
			var winStreak = _winStreak > 2
				                ? string.Format("{0}! {1} win in a row", GetKillingSpree(_winStreak), GetOrdinal(_winStreak))
				                : _lastGame.Result.ToString();
			var deck = DeckList.Instance.ActiveDeck;
			Core.Send(string.Format("{0} vs {1} ({2}) after {3}: {4}", winStreak, _lastGame.OpponentName, _lastGame.OpponentHero.ToLower(),
			                        _lastGame.Duration, deck.WinLossString));
			_lastGame = null;
		}

		private static string GetKillingSpree(int wins)
		{
			var index = wins / 3 - 1;
			if(index < 0)
				return "";
			if(index > 5)
				index = 5;
			return KillingSprees[index];
		}

		//http://www.c-sharpcorner.com/UploadFile/b942f9/converting-cardinal-numbers-to-ordinal-using-C-Sharp/
		private static string GetOrdinal(int number)
		{
			if(number < 0)
				return number.ToString();
			var rem = number % 100;
			if(rem >= 11 && rem <= 13)
				return number + "th";
			switch(number % 10)
			{
				case 1:
					return number + "st";
				case 2:
					return number + "nd";
				case 3:
					return number + "rd";
				default:
					return number + "th";
			}
		}
	}
}