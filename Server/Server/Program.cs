using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;


namespace Dwarrowdelf.Server
{
	class ServerLauncher
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.Name = "Main";

#if DEBUG
			bool debugServer = Properties.Settings.Default.DebugServer;
#else
			bool debugServer = false;
#endif

			Server server = new Server();

			server.RunServer(false, debugServer, null, null);
		}
	}
}