using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public sealed class GameFactory : MarshalByRefObject, IGameFactory
	{
		public IGame CreateGame(string gameDir, GameOptions options)
		{
			MyTraceContext.ThreadTraceContext = new MyTraceContext("Server");

			Trace.TraceInformation("Initializing area");
			var initSw = Stopwatch.StartNew();

			IGame game;

			switch (options.Mode)
			{
				case GameMode.Fortress:
					game = new Fortress.FortressGame(gameDir, options);
					break;

				case GameMode.Adventure:
					game = new Fortress.DungeonGame(gameDir, options);
					break;

				default:
					throw new Exception();
			}

			initSw.Stop();
			Trace.TraceInformation("Initializing area took {0} ms", initSw.ElapsedMilliseconds);

			return game;
		}

		public IGame LoadGame(string gameDir, Guid save)
		{
			MyTraceContext.ThreadTraceContext = new MyTraceContext("Server");

			IGame game = GameEngine.Load(gameDir, save);

			return game;
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
