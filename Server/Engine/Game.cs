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

		public void CreateWorld()
		{
			this.Engine.Create();
		}

		public void LoadWorld(Guid id)
		{
			this.Engine.Load(id);
		}

		public void Run(EventWaitHandle serverStartWaitHandle)
		{
			this.Server.Run(serverStartWaitHandle);
		}

		public void Stop()
		{
			this.Server.Stop();
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}

	public class GameFactory : MarshalByRefObject, IGameFactory
	{
		public IGame CreateGame(string gameAreaName, string gameDir)
		{
			var assembly = LoadGameAssembly(gameAreaName);

			var engine = (GameEngine)assembly.CreateInstance("MyArea.MyEngine", false, BindingFlags.Public | BindingFlags.Instance, null, new object[] { gameDir }, null, null);

			var server = new GameServer(engine);

			var game = new Game(server, engine);

			return game;
		}

		Assembly LoadGameAssembly(string gameAreaName)
		{
			// XXX
#if DEBUG
			const bool debug = true;
#else
			const bool debug = false;
#endif

			var basePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

			var parts = basePath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList();

			parts.RemoveRange(parts.Count - 4, 4);

			parts.Add(Path.Combine(gameAreaName, "bin", debug ? "Debug" : "Release"));

			var path = string.Join(Path.DirectorySeparatorChar.ToString(), parts);

			path = Path.Combine(path, gameAreaName + ".dll");

			var assembly = Assembly.LoadFile(path);
			return assembly;
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
