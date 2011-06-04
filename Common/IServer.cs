using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dwarrowdelf
{
	public interface IGameFactory
	{
		IGame CreateGameAndServer(string gameDll, string gameDir);
	}

	public interface IGame
	{
		void Run(EventWaitHandle serverStartWaitHandle);
		void Stop();
	}

}
