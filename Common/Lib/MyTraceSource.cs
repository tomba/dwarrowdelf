using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace Dwarrowdelf
{
	public sealed class MyTraceSource
	{
		public string Header { get; set; }

		SourceLevels m_sourceLevels;

		public TraceLevel TraceLevel
		{
			get
			{
				switch (m_sourceLevels)
				{
					case SourceLevels.Off:
						return TraceLevel.Off;
					case SourceLevels.Error:
						return TraceLevel.Error;
					case SourceLevels.Warning:
						return TraceLevel.Warning;
					case SourceLevels.Information:
						return TraceLevel.Info;
					case SourceLevels.Verbose:
						return TraceLevel.Verbose;
					default:
						return TraceLevel.Off;
				}
			}

			set
			{
				switch (value)
				{
					case TraceLevel.Off:
						m_sourceLevels = SourceLevels.Off;
						break;

					case TraceLevel.Verbose:
						m_sourceLevels = SourceLevels.Verbose;
						break;

					case TraceLevel.Info:
						m_sourceLevels = SourceLevels.Information;
						break;

					case TraceLevel.Warning:
						m_sourceLevels = SourceLevels.Warning;
						break;

					case TraceLevel.Error:
						m_sourceLevels = SourceLevels.Error;
						break;
				}
			}
		}


		public MyTraceSource(string name, string header = null)
		{
			this.Header = header;

			if (MyTraceSettings.Settings != null)
			{
				var settings = MyTraceSettings.Settings.DefaultTraceLevels[name];

				if (settings != null)
					this.TraceLevel = settings.Level;
			}
		}

		[Conditional("TRACE")]
		public void TraceError(string message)
		{
			Trace(TraceEventType.Error, message);
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
		public void TraceInformation(string message)
		{
			Trace(TraceEventType.Information, message);
		}

		[Conditional("TRACE")]
		public void TraceInformation(string format, params object[] args)
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
			if ((((int)m_sourceLevels) & ((int)eventType)) != 0)
				WriteLine(eventType, message);
		}

		[Conditional("TRACE")]
		void Trace(TraceEventType eventType, string format, params object[] args)
		{
			if ((((int)m_sourceLevels) & ((int)eventType)) != 0)
			{
				if (args == null || args.Length == 0)
					WriteLine(eventType, format);
				else
					WriteLine(eventType, String.Format(format, args));
			}
		}

		void WriteLine(TraceEventType eventType, string message)
		{
			string thread = Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();

			string component;
			int tick;

			var ctx = MyTraceContext.ThreadTraceContext;
			if (ctx != null)
			{
				component = ctx.Component;
				tick = ctx.Tick;
			}
			else
			{
				component = "";
				tick = -1;
			}

			MemoryMappedLog.MMLog.Append(tick, component, thread, this.Header, message);
		}
	}
}
