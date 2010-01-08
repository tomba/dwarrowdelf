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

	public static class MyTrace
	{
		public static void WriteLine(string str)
		{
			MMLog.Append((int)MyDebug.DefaultFlags, str);
		}

		public static void WriteLine(string format, params object[] args)
		{
			WriteLine(String.Format(format, args));
		}
	}

	public class MyDebugListener : TraceListener
	{
		public override void Write(string message)
		{
			MyDebug.WriteLine(message);
		}

		public override void WriteLine(string message)
		{
			MyDebug.WriteLine(message);
		}
	}

	public class MyTraceListener : TraceListener
	{
		public override void Write(string message)
		{
			MyTrace.WriteLine(message);
		}

		public override void WriteLine(string message)
		{
			MyTrace.WriteLine(message);
		}
	}
}
