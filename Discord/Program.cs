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

		public static Dictionary<ulong, TSPlayer> LoggdedInUsers { get; set; }

		private static int _userId = -2;

		public static int UserId
		{
			get
			{
				if (_userId <= int.MinValue)
					_userId = -2;
				return _userId--;
			}
		}

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

			LoggdedInUsers = new Dictionary<ulong, TSPlayer>();

			Commands.InitCommands(isPlugin);

			TShockAPI.Commands.ChatCommands.Add(new TShockAPI.Command("choobot.manage", Bot, "bot"));

			ServerApi.Hooks.ServerChat.Register(this, OnChat);
			ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
		}

		void OnJoin(JoinEventArgs e)
		{
			Task.Run(async () =>
			{
				await discordBot.SendMessage(config.ChatChannelId, $"{TShock.Players[e.Who].Name} has joined.", MarkDown.BoldItalics);
			});
		}

		void OnLeave(LeaveEventArgs e)
		{
			Task.Run(async () =>
			{
				await discordBot.SendMessage(config.ChatChannelId, $"{TShock.Players[e.Who].Name} has left.", MarkDown.BoldItalics);
			});
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
				case "reloadconfig":
					DiscordPlugin.config = Config.Load();
					args.Player.SendInfoMessage($"Config file has been reloaded.");
					break;
				case "help":
				default:
					args.Player.SendInfoMessage($"Invalid syntax! proper syntax: {TShockAPI.Commands.Specifier}bot [enable|disable|restart|reloadconfig]");
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
			await discordBot.Connect(config.BotToken);

			discordBot.Client.MessageReceived += Client_MessageReceived;
		}

		private static void Client_MessageReceived(object sender, MessageEventArgs e)
		{
			if (e.Message.IsAuthor)
				return;

#if DEBUG
			Console.WriteLine(e.Message);
#endif
			if (e.Message.Text.StartsWith(".") && !string.IsNullOrWhiteSpace(e.Message.Text.Substring(1)))
			{
				return;
				if (DiscordPlugin.LoggdedInUsers.ContainsKey(e.User.Id))
				{
					TSPlayer ts;

					if (DiscordPlugin.LoggdedInUsers.TryGetValue(e.User.Id, out ts))
					{
						Console.WriteLine("tryhandle..");
						bool success = Commands.HandleTShockCommand(ts, e.Message.Text);
#if DEBUG
						Console.WriteLine($"Command executed {(success ? "" : "un")}successfully");
#endif
					}
				}
				else
					e.Channel.SendMessage($"You are not logged in!\nPlease private message ChooBot with {Commands.Specifier}login <username> <password> to use TShock commands.");
				return;
			}
			if (e.Message.Text.StartsWith(Commands.Specifier) && !string.IsNullOrWhiteSpace(e.Message.Text.Substring(1)))
			{
				if (!discordBot.Enabled && e.Message.Text.Substring(1, 3).ToLower() != "bot")
					return;
				Commands.HandleCommand(e, e.Message.Text);
				return;
			}
			if (e.Channel.Id == config.ChatChannelId && DiscordPlugin.isPlugin && discordBot.Enabled)
			{
				if (e.Message.Text.Length > 500)
				{
					e.Message.Delete();
					return;
				}
				TShock.Utils.Broadcast($"[Discord] {e.Message}", DiscordPlugin.config.ChatColor.toColor());
			}
		
	}

		protected override void Dispose(bool disposing)
		{
			ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
			discordBot.Client.MessageReceived -= Client_MessageReceived;
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
}
