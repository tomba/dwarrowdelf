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
		Ball,
		Cube,
	}

	public enum WorldTickMethod
	{
		Simultaneous,
		Sequential,
	}

	[Serializable]
	public class GameOptions
	{
		public GameMode Mode;
		public GameMap Map;
		public WorldTickMethod TickMethod;
	}

	public interface IGameFactory
	{
		IGame CreateGame(string gameDir, GameOptions options);
		IGame LoadGame(string gameDir, Guid save);
	}

	public interface IGame
	{
		void Run(EventWaitHandle serverStartWaitHandle);
		void Stop();

		void Signal();

		void Connect(DirectConnection clientConnection);
	}
}
