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
			string thread = Thread.CurrentThread.Name;
			MMLog.Append(m_component ?? "", thread ?? "", message);
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
	}
}
