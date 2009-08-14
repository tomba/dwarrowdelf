using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;
using System.IO;
using System.Diagnostics;


namespace MyGame
{
	class ServerLauncher
	{
		static void Main(string[] args)
		{
			/*
			long t = LOSShadowCast1.PerfTest();
			Console.WriteLine(t);
			return;
			*/
			TraceListener listener = new ConsoleTraceListener();
			Server server = new Server();
			server.RunServer(false, listener, null, null);
		}
	}
}