using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace MyGame
{
	public static class MyDebug
	{
		public static string Prefix { get; set; }

		[MethodImpl(MethodImplOptions.Synchronized)]
		[Conditional("DEBUG")]
		public static void WriteLine(string str)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(DateTime.Now.ToString("hh:mm:ss.ff"));
			sb.Append(" ");

			string prefix = Prefix;
			if (prefix != null)
				sb.Append(prefix);

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
