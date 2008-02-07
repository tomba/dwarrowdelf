using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	public static class MyDebug
	{
		public static string Prefix { get; set; }

		public static void WriteLine(string str)
		{
			if (Prefix != null)
				Debug.WriteLine(Prefix + str);
			else
				Debug.WriteLine(str);
		}

		public static void WriteLine(string format, params object[] args)
		{
			WriteLine(String.Format(format, args));
		}

	}
}
