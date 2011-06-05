using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dwarrowdelf
{
	public interface IGameFactory
	{
		IGame CreateGame(string gameDll, string gameDir);
		IGame LoadGame(string gameDll, string gameDir, string saveFile);
	}

	public interface IGame
	{
		void Run(EventWaitHandle serverStartWaitHandle);
		void Stop();
	}
}
