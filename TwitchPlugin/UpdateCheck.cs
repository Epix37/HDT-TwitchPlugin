﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Hearthstone_Deck_Tracker;
using MahApps.Metro.Controls.Dialogs;

namespace TwitchPlugin
{
	public class UpdateCheck
	{
		public static async void Run(Version current)
		{
			Logger.WriteLine("Checking for updates...", "TwitchPlugin");

			const string versionXmlUrl = @"https://raw.githubusercontent.com/Epix37/HDT-Data/master/Plugins/twitchplugin-version";
			try
			{
				Logger.WriteLine("Current version: " + current, "TwitchPlugin");
				string xml;
				using (var wc = new WebClient())
					xml = await wc.DownloadStringTaskAsync(versionXmlUrl);

				var newVersion = new Version(XmlManager<SerializableVersion>.LoadFromString(xml).ToString());
				Logger.WriteLine("Latest version: " + newVersion, "TwitchPlugin");

				if(newVersion > current)
				{
					await Task.Delay(5000);
					var result = await Helper.MainWindow.ShowMessageAsync("TwitchPlugin update available!", "(Plugins can not be updated automatically)",
					                                   MessageDialogStyle.AffirmativeAndNegative,
					                                   new MetroDialogSettings() {AffirmativeButtonText = "download", NegativeButtonText = "not now"});
					if(result == MessageDialogResult.Affirmative)
						Process.Start(@"https://github.com/Epix37/HDT-TwitchPlugin/releases");

				}
			}
			catch (Exception e)
			{
				Logger.WriteLine("Error checking for new version.\n\n" + e, "TwitchPlugin");
			}
		}
	}
}
