using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordApp
{
	public class ChatColor
	{
		public int R;
		public int G;
		public int B;
		public ChatColor(int r, int g, int b)
		{
			R = r;
			G = g;
			B = b;
		}

		public Color toColor()
		{
			return new Color(R, G, B);
		}
	}
}
