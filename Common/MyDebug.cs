using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MemoryMappedLog;


namespace MyGame
{
	[Flags]
	public enum DebugFlag : int
	{
		None,
		Mark,
		Client,
		Server,
	}

	public static class MyDebug
	{
		public static DebugFlag DefaultFlags { get; set; }

		[Conditional("DEBUG")]
		public static void WriteLine(string str)
		{
			MMLog.Append((int)DefaultFlags, str);
		}

		[Conditional("DEBUG")]
		public static void WriteLine(string format, params object[] args)
		{
			WriteLine(String.Format(format, args));
		}
	}
}
