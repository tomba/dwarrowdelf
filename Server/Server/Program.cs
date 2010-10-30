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

			Server server = new Server();

			server.RunServer(false, null, null);
		}
	}
}