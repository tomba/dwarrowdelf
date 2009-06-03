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

		[Conditional("DEBUG")]
		public static void WriteLine(string str)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(DateTime.Now.ToString("hh:mm:ss.ff"));
			sb.Append(" ");

			if (Prefix != null)
				sb.Append(Prefix);

			sb.Append(str);

			Debug.WriteLine(sb.ToString());
		}

		[Conditional("DEBUG")]
		public static void WriteLine(string format, params object[] args)
		{
			WriteLine(String.Format(format, args));
		}

	}
}
