using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public sealed class GameFactory : MarshalByRefObject, IGameFactory
	{
		public IGame CreateGame(string gameDir, GameMode mode, GameMap map)
		{
			MyTraceContext.ThreadTraceContext = new MyTraceContext("Server");

			WorldTickMethod tickMethod;

			switch (mode)
			{
				case GameMode.Fortress:
					tickMethod = WorldTickMethod.Simultaneous;
					break;

				case GameMode.Adventure:
					tickMethod = WorldTickMethod.Sequential;
					break;

				default:
					throw new Exception();
			}

			var world = new World(mode, tickMethod);

			Action<World> worldCreator;

			switch (map)
			{
				case GameMap.Fortress:
					worldCreator = Fortress.MountainWorldCreator.InitializeWorld;
					break;

				case GameMap.Adventure:
					var dwc = new Fortress.DungeonWorldCreator(world);
					worldCreator = dwc.InitializeWorld;
					break;

				case GameMap.Arena:
					throw new Exception();

				default:
					throw new Exception();
			}

			world.Initialize(delegate
			{
				worldCreator(world);
			});

			var engine = new GameEngine(world, mode);

			InitGame(engine, gameDir);

			return engine;
		}

		public IGame LoadGame(string gameDir, Guid save)
		{
			var engine = GameEngine.Load(gameDir, save);

			InitGame(engine, gameDir);

			return engine;
		}

		void InitGame(GameEngine engine, string gameDir)
		{
			IGameManager gameManager;

			switch (engine.GameMode)
			{
				case GameMode.Fortress:
					gameManager = new Fortress.FortressGameManager(engine.World);
					break;

				case GameMode.Adventure:
					gameManager = new Fortress.DungeonGameManager(engine.World);
					break;

				default:
					throw new Exception();
			}

			engine.Init(gameDir, gameManager);
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
