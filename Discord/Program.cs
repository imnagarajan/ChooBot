using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using TShockAPI;
using TerrariaApi.Server;
using Terraria;
using System.Reflection;
using System.Threading;
using System.IO;

namespace DiscordApp
{
	[ApiVersion(1, 23)]
	public class DiscordPlugin : TerrariaPlugin
	{
		public static bool isPlugin = true;
		public static DiscordBot discordBot;
		public static Config config;
		public static string SavePath => Path.Combine(isPlugin ? TShock.SavePath : Directory.GetCurrentDirectory(), "DiscordPlugin");

		public override string Author => "Ancientgods";
		public override string Name => "DiscordPlugin";
		public override string Description => "";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
	
		public override void Initialize()
		{
			if (!Directory.Exists(SavePath))
				Directory.CreateDirectory(SavePath);
			

			if (File.Exists(Config.SavePath))
				config = Config.Load();
			else
			{
				config = new Config();
				config.Save();
			}

			Task.Run(async () =>
			{
				await StartBot();
			});
	
			Commands.InitCommands(isPlugin);

			TShockAPI.Commands.ChatCommands.Add(new TShockAPI.Command("choobot.manage",Bot, "bot"));

			ServerApi.Hooks.ServerChat.Register(this, OnChat);
		}

		static void Bot(TShockAPI.CommandArgs args)
		{
			string option = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();

			switch (option)
			{
				case "enable":
					DiscordPlugin.discordBot.Enabled = true;
					args.Player.SendInfoMessage($"ChooBot is now enabled.");
					
					break;
				case "disable":
					DiscordPlugin.discordBot.Enabled = false;
					args.Player.SendInfoMessage($"ChooBot is now disabled.");
					break;
				case "restart":
					args.Player.SendInfoMessage("Restarting ChooBot...");
					Task.Run(async () =>
					{
						await DiscordPlugin.StartBot();
					}).ContinueWith((o) =>
					{
						args.Player.SendInfoMessage("ChooBot restarted successfully.");
					});
					break;
				case "help":
				default:
					args.Player.SendInfoMessage($"Invalid syntax! proper syntax: {TShockAPI.Commands.Specifier}bot [enable|disable|restart]");
					break;
			}
		}

		public static async Task StartBot()
		{
			if (discordBot != null)
			{
				discordBot.Dispose();
				discordBot = null;
			}

			discordBot = new DiscordBot(config.ServerId, config.ChatChannelId, config.LogChannelId);
			await discordBot.Connect(config.BotEmail, config.BotPassword);			
		}

		protected override void Dispose(bool disposing)
		{
			ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
			base.Dispose(disposing);
		}

		public static async void Log(string msg)
		{
			await discordBot.SendMessage(config.LogChannelId, msg);
#if DEBUG
			Console.WriteLine(msg);
#endif
		}

		public void OnChat(ServerChatEventArgs e)
		{
			if ((e.Text.StartsWith(TShock.Config.CommandSpecifier) || e.Text.StartsWith(TShock.Config.CommandSilentSpecifier)) && !string.IsNullOrWhiteSpace(e.Text.Substring(1)))
				return;

			var tsplr = TShock.Players[e.Who];

			if (!tsplr.HasPermission(Permissions.canchat))
				return;

			var text = String.Format(TShock.Config.ChatFormat, tsplr.Group.Name, tsplr.Group.Prefix, tsplr.Name, tsplr.Group.Suffix, e.Text);

			Task.Run(action: async () =>
			{
				await discordBot.SendMessage(config.ChatChannelId, text);
#if DEBUG
				Console.WriteLine(text);
#endif
			});
		}

		public DiscordPlugin(Main game) : base(game)
		{
			Order = 1;
		}

		static void Main(string[] args)
		{
			isPlugin = false;
			new DiscordPlugin(null).Initialize();
			Console.ReadLine();
		}
	}

	public class DiscordBot : IDisposable
	{
		public DiscordBot(ulong ServerId, ulong ChatChannelId, ulong LogChannelId)
		{
			this.ServerId = ServerId;
			this.ChatChannelId = ChatChannelId;
			this.LogChannelId = LogChannelId;	
		}

		public bool Enabled { get; set; } = true;
		public DiscordClient Client { get; set; }
		public ulong ServerId { get; private set; }
		public ulong ChatChannelId { get; private set; }
		public ulong LogChannelId { get; private set; }

		public async Task Connect(string username, string password)
		{
			Client = new DiscordClient();
			
			await Client.Connect(username, password).ContinueWith((o)=> 
			{
				Client.MessageReceived += Client_MessageReceived;
				Console.WriteLine("Bot connected to server.");
			});
		}

		private void Client_MessageReceived(object sender, MessageEventArgs e)
		{
			if (e.Message.IsAuthor)
				return;

#if DEBUG
				Console.WriteLine(e.Message);
#endif
			if (e.Message.Text.StartsWith(Commands.Specifier) && !string.IsNullOrWhiteSpace(e.Message.Text.Substring(1)))
			{
				if (!Enabled && e.Message.Text.Substring(1, 3).ToLower() != "bot")
					return;
				Commands.HandleCommand(e, e.Message.Text);
				return;
			}
			if (e.Channel.Id == ChatChannelId && DiscordPlugin.isPlugin && Enabled)
			{
				if (e.Message.Text.Length > 500)
				{
					e.Message.Delete();
					return;
				}
				TShock.Utils.Broadcast($"[Discord] {e.Message}", new Color(242, 44, 196));
			}
		}

		public async Task SendMessage(ulong channelId, string message)
		{
			if (!Enabled)
				return;

			await Client.GetServer(ServerId).GetChannel(channelId).SendMessage(message);
		}

		public void Dispose()
		{
			Client.MessageReceived -= Client_MessageReceived;
			Client = null;
		}
	}
}
