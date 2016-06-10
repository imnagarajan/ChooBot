using System;
using Newtonsoft.Json;
using System.IO;

namespace DiscordApp
{
	public class Config
	{
		public static string SavePath => Path.Combine(DiscordPlugin.SavePath, "config.json");
		public string BotToken;
		public ulong ServerId;
		public ulong ChatChannelId;
		public ulong LogChannelId;
		public ChatColor ChatColor;

		public Config()
		{
			BotToken = "";
			ChatColor = new ChatColor(33, 181, 20);
		}

		public void Save()
		{
			Console.WriteLine(SavePath);
			using (StreamWriter sw = new StreamWriter(File.Open(SavePath, FileMode.Create)))
			{
				sw.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
			}
		}

		public static Config Load()
		{
			using (StreamReader sr = new StreamReader(File.Open(SavePath, FileMode.Open)))
			{
				return JsonConvert.DeserializeObject<Config>(sr.ReadToEnd());
			}
		}
	}
}
