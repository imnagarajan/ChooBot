using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace DiscordApp
{
	public delegate void CommandDelegate(CommandArgs args);

	public static class Commands
	{
		public static List<Command> ChatCommands = new List<Command>();

		public static string Specifier => "!";

		public static void InitCommands(bool isPlugin)
		{
			Action<Command> add = (cmd) =>
			{
				if (!cmd.RequireIsPlugin || isPlugin)
					ChatCommands.Add(cmd);
			};

			#region AddCommands
			add(new Command(Help, "help") { HelpText = "displays a list of commands" });
			add(new Command(Rnd, "rnd")
			{
				HelpText = "displays a random number",
				CmdInfo = new List<string>
				{
					$"{Specifier}rnd - generates a number between 0 and {int.MaxValue -1}",
					$"{Specifier}rnd <high> - generates a number between 0 and 'high'",
					$"{Specifier}rnd <low> <high> - generates a number between 'low' and 'high'"
				}
			});
			add(new Command(Bot, "bot")
			{
				HelpText = "Bot admin commands",
				CmdInfo = new List<string>
				{
					$"{Specifier}bot [enable|disable|restart|reloadconfig]"
				}
			});
			add(new Command(Ping, "ping")
			{
				HelpText = "Ping ChooBot to see if he is still alive."
			});
			add(new Command(Login, "login")
			{
				RequireIsPlugin = true,
				HelpText = "Log in on your TShock account."
			});
			add(new Command(Playing, "playing")
			{
				RequireIsPlugin = true,
				HelpText = "Lists online players (in the server)."
			});
			add(new Command(Staff, "staff")
			{
				RequireIsPlugin = true,
				HelpText = "Lists all online staff members (in the server)."
			});
			#endregion AddCommands
		}

		#region Commands
		static void Staff(CommandArgs args)
		{
			string[] staff = TShock.Players.Where(t => t != null && t.Group.HasPermission("staffchat.staffmember")).OrderBy(t => t.Group.Name).Select(t => t.Group.Prefix + t.Name).ToArray();
			if (staff.Length == 0)
			{
				args.msgEventArgs.Channel.SendMessage("There aren't any staffmembers online at this moment!", MarkDown.CodeLine);
				return;
			}
			string str = $"Currently online staffmembers:\n-----------------------------\n{string.Join("\n", staff)}";
			args.msgEventArgs.Channel.SendMessage(str, MarkDown.CodeBlock);
		}
		static void Playing(CommandArgs args)
		{
			string playing = string.Join(", ", TShock.Players.Where(t => t != null).Select(t => t.Name).ToArray());
			if (string.IsNullOrWhiteSpace(playing))
			{
				args.msgEventArgs.Channel.SendMessage("There aren't any players online at this moment!", MarkDown.CodeLine);
				return;
			}
			args.msgEventArgs.Channel.SendMessage($"Currently online players:\n-------------------------\n{playing}", MarkDown.CodeBlock);
		}

		static void Ping(CommandArgs args)
		{
			args.msgEventArgs.Channel.SendMessage("pong", MarkDown.CodeLine);
		}

		static void Bot(CommandArgs args)
		{
			if (!args.msgEventArgs.User.ServerPermissions.ManageChannels)
			{
				args.msgEventArgs.Channel.SendMessage($"{args.msgEventArgs.User.Name} does not have permission to use {Specifier}bot", MarkDown.CodeLine);
				return;
			}
			string option = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();

			switch (option)
			{
				case "enable":
					DiscordPlugin.discordBot.Enable(args.msgEventArgs.Channel.Id);
					break;
				case "disable":
					DiscordPlugin.discordBot.Disable(args.msgEventArgs.Channel.Id);
					break;
				case "restart":
					ulong channelId = args.msgEventArgs.Channel.Id;
					args.msgEventArgs.Channel.SendMessage("Restarting ChooBot...", MarkDown.CodeLine);
					Task.Run(async () =>
					{
						await DiscordPlugin.StartBot();
					}).ContinueWith(async (o) =>
					{
						await DiscordPlugin.discordBot.SendMessage(channelId, "ChooBot restarted successfully.", MarkDown.CodeLine);
					});
					break;
				case "reloadconfig":
					DiscordPlugin.config = Config.Load();
					args.msgEventArgs.Channel.SendMessage($"Config file has been reloaded.", MarkDown.CodeLine);
					break;
				case "help":
				default:
					args.msgEventArgs.Channel.SendMessage($"Invalid syntax! proper syntax: {Specifier}bot [enable|disable|restart|reloadconfig]", MarkDown.CodeLine);
					break;
			}
		}

		static void Help(CommandArgs args)
		{
			if (args.Parameters.Count == 1)
			{
				Command cmd = ChatCommands.Find(c => c.Name == args.Parameters[0]);
				if (cmd != null)
				{
					if (cmd.CmdInfo != null && cmd.CmdInfo.Count > 0)
						args.msgEventArgs.Channel.SendMessage($"{Specifier}{cmd.Name}:\n{string.Join("\n", cmd.CmdInfo)}", MarkDown.CodeLine);
					else
						args.msgEventArgs.Channel.SendMessage($"No extra help available for {Specifier}{cmd.Name}", MarkDown.CodeLine);
					return;
				}
			}
			List<string> cmdhelp = new List<string>();
			for (int i = 0; i < ChatCommands.Count; i++)
			{
				cmdhelp.Add($"{Specifier}{ChatCommands[i].Name} - {ChatCommands[i].HelpText}");
			}
			args.msgEventArgs.Channel.SendMessage($"List of commands:\n---------------------\n{string.Join("\n", cmdhelp)}\n---------------------\nType {Specifier}help <command> for more info about a command.", MarkDown.CodeBlock);
		}

		static void Rnd(CommandArgs args)
		{
			int low = 0;
			int high = 0;
			int rnd = 0;
			if (args.Parameters.Count == 1)
			{
				int.TryParse(args.Parameters[0], out high);
				rnd = new Random().Next(high);
				args.msgEventArgs.Channel.SendMessage($"Random number (between 0 and {high}): {rnd}", MarkDown.CodeLine);
			}
			else if (args.Parameters.Count == 2)
			{
				int.TryParse(args.Parameters[0], out low);
				int.TryParse(args.Parameters[1], out high);
				rnd = new Random().Next(low, high);
				args.msgEventArgs.Channel.SendMessage($"Random number (between {low} and {high}): {rnd}", MarkDown.CodeLine);
			}
			else
			{
				rnd = new Random().Next();
				args.msgEventArgs.Channel.SendMessage($"Random number: {rnd}", MarkDown.CodeLine);
			}
		}

		static void Login(CommandArgs args)
		{
			args.msgEventArgs.Message.Delete();
			if (!args.msgEventArgs.Channel.IsPrivate)
			{
				args.msgEventArgs.Channel.SendMessage($"Only use {Specifier}login in private message you fool!", MarkDown.CodeLine);
				return;
			}
			if (args.Parameters.Count != 2)
			{
				args.msgEventArgs.User.SendMessage($"Invalid syntax! Proper syntax: {Specifier}login <username> <password>", MarkDown.CodeLine);
				return;
			}
			TShockAPI.DB.User user = TShock.Users.GetUserByName(args.Parameters[0]);
			if (user != null && user.VerifyPassword(args.Parameters[1]))
			{
				if (DiscordPlugin.LoggdedInUsers.ContainsKey(args.msgEventArgs.User.Id))
					DiscordPlugin.LoggdedInUsers.Remove(args.msgEventArgs.User.Id);
				DiscordPlugin.LoggdedInUsers.Add(args.msgEventArgs.User.Id, user.Name);
				args.msgEventArgs.User.SendMessage("Logged in successfully!", MarkDown.CodeLine);
			}
			else
				args.msgEventArgs.User.SendMessage("Invalid username or password!", MarkDown.CodeLine);
		}
		#endregion Commands

		public static bool HandleTShockCommand(TSPlayer player, string text)
		{
			string cmdText = text.Remove(0, 1);
			string cmdPrefix = text[0].ToString();
			bool silent = false;

			if (cmdPrefix == TShockAPI.Commands.SilentSpecifier)
				silent = true;

			var args = ParseParameters(cmdText);
			if (args.Count < 1)
				return false;

			string cmdName = args[0].ToLower();
			args.RemoveAt(0);

			IEnumerable<TShockAPI.Command> cmds = TShockAPI.Commands.ChatCommands.FindAll(c => c.HasAlias(cmdName));
			Console.WriteLine("Check 1");
			if (cmds.Count() == 0)
			{

				//player.SendErrorMessage("Invalid command entered. Type {0}help for a list of valid commands.", Specifier);
				return true;
			}

			Console.WriteLine("Check 2");
			foreach (TShockAPI.Command cmd in cmds)
			{
				if (!cmd.CanRun(player))
				{
					Console.WriteLine("Check 3");
					TShock.Utils.SendLogs(string.Format("{0} tried to execute {1}{2}.", player.Name, Specifier, cmdText), Color.PaleVioletRed, player);
					player.SendErrorMessage("You do not have access to this command.");
				}
				else if (!cmd.AllowServer && !player.RealPlayer)
				{
					Console.WriteLine("Check 4");
					player.SendErrorMessage("You must use this command in-game.");
				}
				else
				{
					Console.WriteLine("Check 5");
					if (cmd.DoLog)
						TShock.Utils.SendLogs(string.Format("{0} executed: {1}{2}.", player.Name, silent ? TShockAPI.Commands.SilentSpecifier : TShockAPI.Commands.Specifier, cmdText), Color.PaleVioletRed, player);
					cmd.Run(cmdText, silent, player, args);
				}
			}
			return true;
		}

		public static bool HandleCommand(MessageEventArgs e, string text)
		{
			string cmdText = text.Remove(0, 1);
			string cmdPrefix = text[0].ToString();

			var args = ParseParameters(cmdText);
			if (args.Count < 1)
				return false;

			string cmdName = args[0].ToLower();
			args.RemoveAt(0);

			IEnumerable<Command> cmds = ChatCommands.FindAll(c => c.Name == cmdName);

			if (cmds.Count() == 0)
			{
				e.Channel.SendMessage($"Invalid command entered. Type {Specifier}help for a list of valid commands.", MarkDown.CodeLine);
				return true;
			}
			foreach (Command cmd in cmds)
			{
				if (!cmd.CanRun(e))
				{
					e.Channel.SendMessage("You do not have access to this command.", MarkDown.CodeLine);
				}
				else
				{
					DiscordPlugin.Log($"{e.User.Name} executed: {Specifier}{cmdText}");
					cmd.Run(cmdText, e, args);
				}
			}
			return true;
		}

		private static List<string> ParseParameters(string str)
		{
			var ret = new List<string>();
			var sb = new StringBuilder();
			bool instr = false;
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];

				if (c == '\\' && ++i < str.Length)
				{
					if (str[i] != '"' && str[i] != ' ' && str[i] != '\\')
						sb.Append('\\');
					sb.Append(str[i]);
				}
				else if (c == '"')
				{
					instr = !instr;
					if (!instr)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
					else if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else if (IsWhiteSpace(c) && !instr)
				{
					if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else
					sb.Append(c);
			}
			if (sb.Length > 0)
				ret.Add(sb.ToString());

			return ret;
		}

		private static bool IsWhiteSpace(char c) => c == ' ' || c == '\t' || c == '\n';
	}

	public class CommandArgs : EventArgs
	{
		public string Message { get; private set; }
		public List<string> Parameters { get; private set; }
		public MessageEventArgs msgEventArgs { get; set; }

		public CommandArgs(string message, MessageEventArgs e, List<string> parameters)
		{
			Message = message;
			msgEventArgs = e;
			Parameters = parameters;
		}
	}

	public class Command
	{
		public string Name { get; set; }
		public string HelpText { get; set; }
		public List<string> CmdInfo { get; set; }
		public bool RequireIsPlugin { get; set; } = false;

		private CommandDelegate commandDelegate;
		public CommandDelegate CommandDelegate
		{
			get { return commandDelegate; }
			set
			{
				if (value == null)
					throw new ArgumentNullException();

				commandDelegate = value;
			}
		}

		public Command(CommandDelegate cmd, string name)
		{
			commandDelegate = cmd;
			Name = name;
		}

		public bool Run(string msg, MessageEventArgs e, List<string> parms)
		{
			if (!CanRun(e))
				return false;
			try
			{
				CommandDelegate(new CommandArgs(msg, e, parms));
			}
			catch (Exception ex)
			{
				Console.WriteLine("Command failed:");
				Console.WriteLine(ex.ToString());
			}
			return true;
		}

		internal bool CanRun(MessageEventArgs e)
		{
			return true;
		}
	}
}
