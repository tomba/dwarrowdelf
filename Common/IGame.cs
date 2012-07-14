using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dwarrowdelf
{
	public enum GameMode
	{
		Undefined,
		Fortress,
	}

	public interface IGameFactory
	{
		IGame CreateGame(GameMode mode, string gameDir);
	}

	public interface IGame
	{
		void CreateWorld();
		void LoadWorld(Guid id);

		void Run(EventWaitHandle serverStartWaitHandle);
		void Stop();

		void Connect(DirectConnection clientConnection);
	}
}
