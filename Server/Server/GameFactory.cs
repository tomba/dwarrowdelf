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

			World world;

			switch (mode)
			{
				case GameMode.Fortress:
					world = new World(WorldTickMethod.Simultaneous);
					break;

				case GameMode.Adventure:
					world = new World(WorldTickMethod.Sequential);
					break;

				default:
					throw new Exception();
			}

			Action<World> worldCreator;

			switch (map)
			{
				case GameMap.Fortress:
					//worldCreator = Fortress.MountainWorldCreator.InitializeWorld;
					worldCreator = Fortress.DungeonWorldCreator.InitializeWorld;
					break;

				case GameMap.Adventure:
					throw new Exception();

				case GameMap.Arena:
					throw new Exception();

				default:
					throw new Exception();
			}

			world.Initialize(delegate
			{
				worldCreator(world);
			});

			IGameManager gameManager;

			switch (mode)
			{
				case GameMode.Fortress:
					//gameManager = new Fortress.FortressGameManager(world);
					gameManager = new Fortress.DungeonGameManager(world);
					break;

				case GameMode.Adventure:
					throw new NotImplementedException();

				default:
					throw new Exception();
			}

			return GameEngine.Create(gameDir, world, gameManager);
		}

		public IGame LoadGame(string gameDir, Guid save)
		{
			return GameEngine.Load(gameDir, save);
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
