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

			discordBot = new DiscordBot(config.ServerId, config.ChatChannelId, config.LogChannelId);

			Task.Run(action: async () =>
			{
				await discordBot.Connect(config.BotEmail, config.BotPassword);			
			});		

			Commands.InitCommands(isPlugin);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);
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

	public class DiscordBot
	{

		public DiscordBot(ulong ServerId, ulong ChatChannelId, ulong LogChannelId)
		{
			this.ServerId = ServerId;
			this.ChatChannelId = ChatChannelId;
			this.LogChannelId = LogChannelId;	
		}

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
				Commands.HandleCommand(e, e.Message.Text);
			}

			else if (e.Channel.Id ==  ChatChannelId && DiscordPlugin.isPlugin)
			{
				if (e.Message.Text.Length > 500)
				{
					e.Message.Delete();
					return;
				}
				for (int i = 0; i < TShock.Players.Length; i++)
					TShock.Players[i].SendMessage($"[Discord] {e.Message}", new Color(242, 44, 196));
			}
		}

		public async Task SendMessage(ulong channelId, string message)
		{
			await Client.GetServer(ServerId).GetChannel(channelId).SendMessage(message);
		}

		~DiscordBot()
		{
			Client.MessageReceived -= Client_MessageReceived;
		}
	}

	/*public class DiscordBot
	{
		public bool isPlugin { get; set; }
		public bool isConnected { get; set; }
		//static void Main(string[] args) => new DiscordBot().Start();

		public static DiscordClient _client;
		public static Server TerraPix => _client.GetServer(121262275423240192);
		public static  Channel LogChannel => TerraPix.GetChannel(189795004636594176);
		public static ulong LobbyId => 189813404247130112;
		public static Channel ServerChannel => TerraPix.GetChannel(LobbyId);

		public void Start()
		{
			Commands.InitCommands();

			_client = new DiscordClient();

			_client.MessageReceived +=  (s, e) =>
			{			
				if (e.Message.IsAuthor)
					return;

				if (e.Message.Text.StartsWith(Commands.Specifier) && !string.IsNullOrWhiteSpace(e.Message.Text.Substring(1)))
				{
					Commands.HandleCommand(e, e.Message.Text);
				}
				
				else if (e.Channel.Id == LobbyId && isPlugin)
				{					
					if (e.Message.Text.Length > 500)
					{
						e.Message.Delete();
						return;
					}
					for (int i = 0; i < TShock.Players.Length; i++)
						TShock.Players[i].SendMessage($"[Discord] {e.Message}", new Color(242,44,196));					
				}
			};

			_client.ExecuteAndWait(async () =>
			{
				await _client.Connect("bot@terrapix.ca", "poopbot123").ContinueWith((e) => 
				{
					isConnected = true;
					Console.WriteLine("Bot connected to server.");
				});				
			});		
		}

		public static void Log(string msg)
		{
			LogChannel.SendMessage(msg);
		}
	}*/
}
