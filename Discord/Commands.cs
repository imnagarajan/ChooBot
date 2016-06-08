using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
				{
					Console.WriteLine(cmd.Name);
					ChatCommands.Add(cmd);
				}
			};

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
			add(new Command(Login, "login")
			{
				RequireIsPlugin = true
			});
		}

		static void Help(CommandArgs args)
		{
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
