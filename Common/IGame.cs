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
		Adventure,
	}

	public enum GameMap
	{
		Undefined,
		Fortress,
		Adventure,
		Arena,
	}

	public interface IGameFactory
	{
		IGame CreateGame(string gameDir, GameMode mode, GameMap map);
		IGame LoadGame(string gameDir, Guid save);
	}

	public interface IGame
	{
		void Run(EventWaitHandle serverStartWaitHandle);
		void Stop();

		void Connect(DirectConnection clientConnection);
	}
}
