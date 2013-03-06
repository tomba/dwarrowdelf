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

		public SourceLevels TraceLevels { get; set; }

		public MyTraceSource(string name, string header = null)
		{
			this.Header = header;

			if (MyTraceSettings.Settings != null)
			{
				var settings = MyTraceSettings.Settings.DefaultTraceLevels[name];

				if (settings != null)
				{
					switch (settings.Level)
					{
						case TraceLevel.Off:
							this.TraceLevels = SourceLevels.Off;
							break;

						case TraceLevel.Verbose:
							this.TraceLevels = SourceLevels.Verbose;
							break;

						case TraceLevel.Info:
							this.TraceLevels = SourceLevels.Information;
							break;

						case TraceLevel.Warning:
							this.TraceLevels = SourceLevels.Warning;
							break;

						case TraceLevel.Error:
							this.TraceLevels = SourceLevels.Error;
							break;
					}
				}
			}
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
			if ((((int)this.TraceLevels) & ((int)eventType)) != 0)
			{
				WriteLine(message);
			}
		}

		[Conditional("TRACE")]
		void Trace(TraceEventType eventType, string format, params object[] args)
		{
			if ((((int)this.TraceLevels) & ((int)eventType)) != 0)
			{
				if (args == null || args.Length == 0)
				{
					WriteLine(format);
				}
				else
				{
					WriteLine(format, args);
				}
			}
		}

		void WriteLine(string format, params object[] args)
		{
			WriteLine(String.Format(format, args));
		}

		void WriteLine(string message)
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
