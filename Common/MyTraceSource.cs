using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public class MyTraceSource : TraceSource
	{
		public string Header { get; set; }

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
		public void TraceWarning(string format, params object[] args)
		{
			Trace(TraceEventType.Warning, format, args);
		}

		[Conditional("TRACE")]
		public new void TraceInformation(string format, params object[] args)
		{
			Trace(TraceEventType.Information, format, args);
		}

		[Conditional("TRACE")]
		public void TraceVerbose(string format, params object[] args)
		{
			Trace(TraceEventType.Verbose, format, args);
		}

		[Conditional("TRACE")]
		void Trace(TraceEventType eventType, string format, params object[] args)
		{
			var sb = new StringBuilder(this.Header);
			sb.Append(": ");
			sb.AppendFormat(format, args);
			TraceEvent(eventType, 0, sb.ToString());
		}
	}
}
