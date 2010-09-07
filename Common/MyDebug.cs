using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using MemoryMappedLog;
using System.Threading;
using System.Diagnostics;

namespace MyGame
{
	public static class MyDebug
	{
		static MyDebug()
		{
			Component = "";
		}

		public static string Component { get; set; }

		[Conditional("DEBUG")]
		public static void WriteLine(string str)
		{
			string thread = Thread.CurrentThread.Name;
			MMLog.Append(Component, thread != null ? thread : "", str);
		}

		[Conditional("DEBUG")]
		public static void WriteLine(string format, params object[] args)
		{
			WriteLine(String.Format(format, args));
		}

		[Conditional("DEBUG")]
		public static void Assert(bool p)
		{
			if (!p)
				Debugger.Break();
		}
	}

	public static class MyTrace
	{
		public static void WriteLine(string str)
		{
			string thread = Thread.CurrentThread.Name;
			MMLog.Append(MyDebug.Component, thread != null ? thread : "", str);
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
