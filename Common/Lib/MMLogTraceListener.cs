using System;
using System.Diagnostics;
using System.Threading;

using MemoryMappedLog;

namespace Dwarrowdelf
{
	public sealed class MyTraceContext
	{
		[ThreadStatic]
		public static MyTraceContext ThreadTraceContext;

		public MyTraceContext(string component)
		{
			this.Component = component;
		}

		public readonly string Component;
		public int Tick;
	}

	public sealed class MMLogTraceListener : TraceListener
	{
		public MMLogTraceListener()
		{
		}

		public override bool IsThreadSafe { get { return true; } }

		public override void Write(string message)
		{
			WriteLine(message);
		}

		public override void WriteLine(string message)
		{
			string thread = Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();

			string component = "";
			int tick = -1;

			var ctx = MyTraceContext.ThreadTraceContext;
			if (ctx != null)
			{
				component = ctx.Component;
				tick = ctx.Tick;
			}

			MMLog.Append(tick, component, thread, "", message);
		}

		public override void Fail(string message)
		{
			if (Debugger.IsAttached)
				Debugger.Break();

			throw new Exception();
		}

		public override void Fail(string message, string detailMessage)
		{
			if (Debugger.IsAttached)
				Debugger.Break();

			throw new Exception();
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			WriteLine(message);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			if (args == null || args.Length == 0)
				WriteLine(format);
			else
				WriteLine(String.Format(format, args));
		}
	}
}
