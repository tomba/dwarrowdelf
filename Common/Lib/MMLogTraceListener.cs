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
			switch (component)
			{
				case "Server":
					this.Component = LogComponent.Server;
					break;

				case "Client":
					this.Component = LogComponent.Client;
					break;

				default:
					throw new Exception();
			}
		}

		public readonly LogComponent Component;
		public int Tick;
	}

	public sealed class MMLogTraceListener : TraceListener
	{
		public MMLogTraceListener()
		{
		}

		public override bool IsThreadSafe { get { return true; } }

		void Append(TraceEventType eventType, string message)
		{
			string thread = Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();

			LogComponent component;
			int tick;

			var ctx = MyTraceContext.ThreadTraceContext;
			if (ctx != null)
			{
				component = ctx.Component;
				tick = ctx.Tick;
			}
			else
			{
				component = LogComponent.None;
				tick = -1;
			}

			MMLog.Append(eventType, tick, component, thread, "", message);
		}

		public override void Write(string message)
		{
			Append(TraceEventType.Information, message);
		}

		public override void WriteLine(string message)
		{
			Append(TraceEventType.Information, message);
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
			Append(eventType, message);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			if (args == null || args.Length == 0)
				Append(eventType, format);
			else
				Append(eventType, String.Format(format, args));
		}
	}
}
