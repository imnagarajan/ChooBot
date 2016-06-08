using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
					$"{Specifier}bot [enable|disable|restart]"
				}
			});
			add(new Command(Ping, "ping")
			{
				HelpText = "Ping ChooBot to see if he is still alive."
			});
			add(new Command(Login, "login")
			{
				RequireIsPlugin = true
			});
			#endregion AddCommands
		}

		#region Commands
		static void Ping(CommandArgs args)
		{	
			args.msgEventArgs.Channel.SendMessage("pong");
		}

		static void Bot(CommandArgs args)
		{
			if (!args.msgEventArgs.User.ServerPermissions.ManageChannels)
			{
				args.msgEventArgs.Channel.SendMessage($"{args.msgEventArgs.User.Name} does not have permission to use {Specifier}bot");
				return;
			}
			string option = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();

			switch (option)
			{
				case "enable":
					DiscordPlugin.discordBot.Enabled = true;
					args.msgEventArgs.Channel.SendMessage($"ChooBot is now enabled.");
					break;
				case "disable":
					DiscordPlugin.discordBot.Enabled = false;
					args.msgEventArgs.Channel.SendMessage($"ChooBot is now disabled.");
					break;
				case "restart":
					ulong channelId = args.msgEventArgs.Channel.Id;
					args.msgEventArgs.Channel.SendMessage("Restarting ChooBot...");
					Task.Run(async () =>
					{
						await DiscordPlugin.StartBot();
					}).ContinueWith(async (o) => 
					{
						await DiscordPlugin.discordBot.SendMessage(channelId, "ChooBot restarted successfully.");
					});					
					break;
				case "help":
				default:
					args.msgEventArgs.Channel.SendMessage($"Invalid syntax! proper syntax: {Specifier}bot [enable|disable|restart]");
					break;
			}
		}

		static void Help(CommandArgs args)
		{
			Console.WriteLine("poop");
			if (args.Parameters.Count == 1)
			{
				Command cmd = ChatCommands.Find(c => c.Name == args.Parameters[0]);
				if (cmd != null)
				{
					if (cmd.CmdInfo != null && cmd.CmdInfo.Count > 0)
						args.msgEventArgs.Channel.SendMessage($"{Specifier}{cmd.Name}:\n{string.Join("\n", cmd.CmdInfo)}");
					else
						args.msgEventArgs.Channel.SendMessage($"No extra help available for {Specifier}{cmd.Name}");
					return;
				}
			}
			List<string> cmdhelp = new List<string>();
			for (int i = 0; i < ChatCommands.Count; i++)
			{
				cmdhelp.Add($"{Specifier}{ChatCommands[i].Name} - {ChatCommands[i].HelpText}");
			}
			args.msgEventArgs.Channel.SendMessage($"List of commands:\n---------------------\n{string.Join("\n", cmdhelp)}\n---------------------\nType {Specifier}help <command> for more info about a command.");
		}

		static void Rnd(CommandArgs args)
		{
			int low=0;
			int high=0;
			int rnd = 0;
			if (args.Parameters.Count == 1)
			{
				int.TryParse(args.Parameters[0], out high);
				rnd = new Random().Next(high);
				args.msgEventArgs.Channel.SendMessage($"Random number (between 0 and {high}): {rnd}");
			}
			else if (args.Parameters.Count == 2)
			{
				int.TryParse(args.Parameters[0], out low);
				int.TryParse(args.Parameters[1], out high);
				rnd = new Random().Next(low, high);
				args.msgEventArgs.Channel.SendMessage($"Random number (between {low} and {high}): {rnd}");
			}
			else
			{
				rnd = new Random().Next();
				args.msgEventArgs.Channel.SendMessage($"Random number: {rnd}");
			}
		}

		static void Login(CommandArgs args)
		{
			if(!args.msgEventArgs.Channel.IsPrivate)
			{
				args.msgEventArgs.Message.Delete();
				args.msgEventArgs.Channel.SendMessage($"Only use {Specifier}login in private message you fool!");
				return;
			}
		}
		#endregion Commands

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
				e.Channel.SendMessage($"Invalid command entered. Type {Specifier}help for a list of valid commands.");
				return true;
			}
			foreach (Command cmd in cmds)
			{
				if (!cmd.CanRun(e))
				{
					e.Channel.SendMessage("You do not have access to this command.");
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

		public bool Run(string msg, MessageEventArgs e,List<string> parms)
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
