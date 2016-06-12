using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordApp
{
	public static class Utils
	{
		public static void SendMessage(this Channel channel, string message, MarkDown markDown = MarkDown.None)
		{
			Task.Run(async () =>
			{
				await DiscordPlugin.discordBot.SendMessage(channel.Id, message, markDown);
			});
		}

		public static void SendMessage(this User user, string message, MarkDown markDown = MarkDown.None)
		{
			Task.Run(async () =>
			{
				await DiscordPlugin.discordBot.SendMessage(user, message, markDown);
			});
		}
	}
}
