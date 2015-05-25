﻿#region

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Plugins;

#endregion

namespace TwitchPlugin
{
	public class TwitchPlugin : IPlugin
	{
		private MenuItem _menuItem;
		private SettingsWindow _settingsWindow;

		public void OnLoad()
		{
			Setup();
			if(_menuItem == null)
				GenerateMenuItem();
			Hearthstone_Deck_Tracker.API.GameEvents.OnGameEnd.Add(ChatCommands.OnGameEnd);
			Hearthstone_Deck_Tracker.API.GameEvents.OnInMenu.Add(ChatCommands.OnInMenu);
		}

		private void Setup()
		{
			if(!DeckList.Instance.AllTags.Contains(Core.TwitchTag))
			{
				DeckList.Instance.AllTags.Add(Core.TwitchTag);
				DeckList.Save();
				Helper.MainWindow.ReloadTags();
			}
		}

		public void OnUnload()
		{
			if(_settingsWindow != null)
			{
				_settingsWindow.Close();
			}
			Core.Disconnect();
		}

		public void OnButtonPress()
		{
			OpenSettings();
		}

		public void OnUpdate()
		{
		}

		public string Name
		{
			get { return "TwitchPlugin"; }
		}

		public string Description
		{
			get { return "Connects to Twitch IRC to post your decks, stats and more on command.\n\nIf you have questions, suggestions or just want to talk feel free to email me: epikz37@gmail.com."; }
		}

		public string ButtonText
		{
			get { return "Settings"; }
		}

		public string Author
		{
			get { return "Epix"; }
		}

		public Version Version
		{
			get { return new Version(0, 1); }
		}

		public MenuItem MenuItem
		{
			get { return _menuItem; }
		}

		private void GenerateMenuItem()
		{
            _menuItem = new MenuItem { Header = "TWITCH" };
			var connectMenuItem = new MenuItem {Header = "CONNECT"};
			var disconnectMenuItem = new MenuItem {Header = "DISCONNECT", Visibility = Visibility.Collapsed};
			var settingsMenuItem = new MenuItem { Header = "SETTINGS" };

			connectMenuItem.Click += (sender, args) =>
			{
				try
				{
					if(string.IsNullOrEmpty(Config.Instance.User) || string.IsNullOrEmpty(Config.Instance.Channel) ||
					   string.IsNullOrEmpty(Config.Instance.OAuth))
					{
						OpenSettings();
					}
					else if(Core.Connect())
					{
						disconnectMenuItem.Header = string.Format("DISCONNECT ({0}: {1})", Config.Instance.User, Config.Instance.Channel);
						disconnectMenuItem.Visibility = Visibility.Visible;
						connectMenuItem.Visibility = Visibility.Collapsed;
					}
				}
				catch(Exception ex)
				{
					Logger.WriteLine("Error connecting to irc: " + ex);
				}
			};
			disconnectMenuItem.Click += (sender, args) =>
			{
				Core.Disconnect();
				disconnectMenuItem.Visibility = Visibility.Collapsed;
				connectMenuItem.Visibility = Visibility.Visible;
			};
			settingsMenuItem.Click += (sender, args) => OpenSettings();

			_menuItem.Items.Add(connectMenuItem);
			_menuItem.Items.Add(disconnectMenuItem);
			_menuItem.Items.Add(settingsMenuItem);
		}

		private void OpenSettings()
		{
			if(_settingsWindow == null)
			{
				_settingsWindow = new SettingsWindow();
				_settingsWindow.Closed += (sender1, args1) => { _settingsWindow = null; };
				_settingsWindow.Show();
				if(_settingsWindow.CommandsList.Items.Count == 0)
				{
					_settingsWindow.Close();
					_settingsWindow = new SettingsWindow();
					_settingsWindow.Show();
				}
			}
			else
				_settingsWindow.Activate();
		}
	}
}