using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dwarrowdelf
{
	public interface IServer
	{
		void RunServer(EventWaitHandle serverStartWaitHandle, EventWaitHandle serverStopWaitHandle);
	}

	public interface IServerFactory
	{
		IServer CreateGameAndServer(string gameDll, string gameDir);
	}

}
