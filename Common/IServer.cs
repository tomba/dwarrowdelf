using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace MyGame
{
	public interface IServer
	{
		TraceListener TraceListener { set; }

		void RunServer(bool isEmbedded, EventWaitHandle serverStartWaitHandle,
			EventWaitHandle serverStopWaitHandle);
	}
}
