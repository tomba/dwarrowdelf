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

			bool debugServer = Properties.Settings.Default.DebugServer;

			Server server = new Server();

			if (debugServer)
			{
				TraceListener listener = new ConsoleTraceListener();
				server.TraceListener = listener;
			}

			server.RunServer(false, null, null);
		}
	}
}