using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace DiscordApp
{
	public class DiscordBot : IDisposable
	{
		public DiscordBot(ulong ServerId, ulong ChatChannelId, ulong LogChannelId)
		{
			this.ServerId = ServerId;
			this.ChatChannelId = ChatChannelId;
			this.LogChannelId = LogChannelId;
		}

		public DiscordClient Client { get; private set; }
		public bool Enabled { get; set; } = true;
		public ulong ServerId { get; private set; }
		public ulong ChatChannelId { get; private set; }
		public ulong LogChannelId { get; private set; }

		public async Task Connect(string token)
		{
			Client = new DiscordClient();

			await Client.Connect(token).ContinueWith((o) =>
			{
				Console.WriteLine("Bot connected to server.");
			});
		}

		public async Task SendMessage(ulong channelId, string message, MarkDown markDown = MarkDown.None)
		{
			if (!Enabled)
				return;

			string[] Markdowns = { "", "*", "**", "***", "~~", "__", "__*", "__**", "__***", "`", "```" };
			await Client.GetServer(ServerId).GetChannel(channelId).SendMessage($"{Markdowns[(int)markDown]}{message}{Markdowns[(int)markDown]}");
		}

		public async Task SendMessage(User user, string message, MarkDown markDown = MarkDown.None)
		{
			if (!Enabled)
				return;
			string[] Markdowns = { "", "*", "**", "***", "~~", "__", "__*", "__**", "__***", "`", "```" };
			await user.SendMessage($"{Markdowns[(int)markDown]}{message}{Markdowns[(int)markDown]}");
		}

		public void Disable(ulong ChannelId)
		{
			if (!Enabled)
				return;

			Enabled = false;
			Task.Run(async () =>
			{
				await Client.GetServer(ServerId).GetChannel(ChannelId).SendMessage("`Choobot is now disabled.`");
			});
		}

		public void Enable(ulong ChannelId)
		{
			if (Enabled)
				return;
			Enabled = false;
			Task.Run(async () =>
			{
				await Client.GetServer(ServerId).GetChannel(ChannelId).SendMessage("`Choobot is now enabled.`");
			});
		}

		public void Dispose()
		{
			Client = null;
		}
	}

	public enum MarkDown
	{
		None,
		Italics,
		Bold,
		BoldItalics,
		Strikeout,
		Underline,
		UnderlineItalics,
		UnderlineBold,
		UnderlineBoldItalics,
		CodeLine,
		CodeBlock
	}
}
