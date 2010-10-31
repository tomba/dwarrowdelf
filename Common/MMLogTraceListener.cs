using System;
using System.Diagnostics;
using System.Threading;

using MemoryMappedLog;

namespace Dwarrowdelf
{
	public class MMLogTraceListener : TraceListener
	{
		string m_component;

		public MMLogTraceListener()
		{
		}

		public override bool IsThreadSafe { get { return true; } }

		public MMLogTraceListener(string component)
		{
			m_component = component;
		}

		public override void Write(string message)
		{
			WriteLine(message);
		}

		public override void WriteLine(string message)
		{
			string thread = Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();
			MMLog.Append(m_component ?? "", thread, message);
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
			if (args == null)
				WriteLine(format);
			else
				WriteLine(String.Format(format, args));
		}

	}

	public static class TraceExtensions
	{
		[Conditional("TRACE")]
		public static void TraceError(this TraceSource traceSource, string format, params object[] args)
		{
			traceSource.TraceEvent(TraceEventType.Error, 0, format, args);
		}

		[Conditional("TRACE")]
		public static void TraceVerbose(this TraceSource traceSource, string format, params object[] args)
		{
			traceSource.TraceEvent(TraceEventType.Verbose, 0, format, args);
		}
	}
}
