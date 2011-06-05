using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;

namespace Dwarrowdelf.Server
{
	public class Game : MarshalByRefObject, IGame
	{
		public GameServer Server { get; private set; }
		public GameEngine Engine { get; private set; }

		public Game(GameServer server, GameEngine engine)
		{
			this.Server = server;
			this.Engine = engine;
		}

		public void Run(EventWaitHandle serverStartWaitHandle)
		{
			this.Server.Run(serverStartWaitHandle);
		}

		public void Stop()
		{
			this.Server.Stop();
		}
	}

	public class GameFactory : MarshalByRefObject, IGameFactory
	{
		public IGame CreateGameAndServer(string gameDll, string gameDir)
		{
			var basePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

			var path = Path.Combine(basePath, gameDll);

			var assembly = Assembly.LoadFile(path);
			var engine = (GameEngine)assembly.CreateInstance("MyArea.MyEngine", false, BindingFlags.Public | BindingFlags.Instance, null, new object[] { gameDir }, null, null);

			var server = new GameServer(engine);

			var game = new Game(server, engine);

			return game;
		}
	}
}
