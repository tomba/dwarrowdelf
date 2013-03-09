using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{

	public sealed class MyTraceProxy
	{
		MyTraceSource m_traceSource;

		// XXX origin not used currently
		public MyTraceProxy(MyTraceSource traceSource, string origin)
		{
			m_traceSource = traceSource;
		}

		[Conditional("TRACE")]
		public void TraceError(string message)
		{
			if (m_traceSource != null)
				m_traceSource.TraceError(message);
		}

		[Conditional("TRACE")]
		public void TraceError(string format, params object[] args)
		{
			if (m_traceSource != null)
				m_traceSource.TraceError(format, args);
		}

		[Conditional("TRACE")]
		public void TraceWarning(string message)
		{
			if (m_traceSource != null)
				m_traceSource.TraceWarning(message);
		}

		[Conditional("TRACE")]
		public void TraceWarning(string format, params object[] args)
		{
			if (m_traceSource != null)
				m_traceSource.TraceWarning(format, args);
		}

		[Conditional("TRACE")]
		public void TraceInformation(string message)
		{
			if (m_traceSource != null)
				m_traceSource.TraceInformation(message);
		}

		[Conditional("TRACE")]
		public void TraceInformation(string format, params object[] args)
		{
			if (m_traceSource != null)
				m_traceSource.TraceInformation(format, args);
		}

		[Conditional("TRACE")]
		public void TraceVerbose(string message)
		{
			if (m_traceSource != null)
				m_traceSource.TraceVerbose(message);
		}

		[Conditional("TRACE")]
		public void TraceVerbose(string format, params object[] args)
		{
			if (m_traceSource != null)
				m_traceSource.TraceVerbose(format, args);
		}
	}
}
