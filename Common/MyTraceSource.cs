using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public sealed class MyTraceSource : TraceSource
	{
		string m_header;
		public string Header { set { m_header = "[" + value + "] "; } }

		public MyTraceSource(string name, string header = null)
			: base(name)
		{
			this.Header = header;
		}

		[Conditional("TRACE")]
		public void TraceError(string format, params object[] args)
		{
			Trace(TraceEventType.Error, format, args);
		}

		[Conditional("TRACE")]
		public void TraceWarning(string message)
		{
			Trace(TraceEventType.Warning, message);
		}

		[Conditional("TRACE")]
		public void TraceWarning(string format, params object[] args)
		{
			Trace(TraceEventType.Warning, format, args);
		}

		[Conditional("TRACE")]
		public new void TraceInformation(string message)
		{
			Trace(TraceEventType.Information, message);
		}

		[Conditional("TRACE")]
		public new void TraceInformation(string format, params object[] args)
		{
			Trace(TraceEventType.Information, format, args);
		}

		[Conditional("TRACE")]
		public void TraceVerbose(string message)
		{
			Trace(TraceEventType.Verbose, message);
		}

		[Conditional("TRACE")]
		public void TraceVerbose(string format, params object[] args)
		{
			Trace(TraceEventType.Verbose, format, args);
		}

		[Conditional("TRACE")]
		void Trace(TraceEventType eventType, string message)
		{
			if (this.Switch.ShouldTrace(eventType) && this.Listeners != null)
			{
				TraceEvent(eventType, 0, m_header + message);
			}
		}

		[Conditional("TRACE")]
		void Trace(TraceEventType eventType, string format, params object[] args)
		{
			if (this.Switch.ShouldTrace(eventType) && this.Listeners != null)
			{
				if (args == null || args.Length == 0)
				{
					TraceEvent(eventType, 0, m_header + format);
				}
				else
				{
					TraceEvent(eventType, 0, m_header + format, args);
				}
			}
		}
	}
}
