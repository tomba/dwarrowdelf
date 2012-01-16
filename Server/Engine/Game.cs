using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;

namespace Dwarrowdelf.Server
{
	public sealed class Game : MarshalByRefObject, IGame
	{
		public GameServer Server { get; private set; }
		public GameEngine Engine { get; private set; }
		public IArea Area { get; private set; }

		public string GameAreaName { get; private set; }
		public string GameDir { get; private set; }

		public Game(string gameAreaName, string gameDir)
		{
			var assembly = LoadGameAssembly(gameAreaName);

			this.Area = (IArea)assembly.CreateInstance("MyArea.Area");

			this.Engine = new GameEngine(this, gameDir);

			this.Server = new GameServer(this.Engine);
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

		Assembly LoadGameAssembly(string gameAreaName)
		{
			var basePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

			string path;

			path = Path.Combine(basePath, gameAreaName + ".dll");

			if (!File.Exists(path))
			{
#if DEBUG
				const bool debug = true;
#else
				const bool debug = false;
#endif
				var parts = basePath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList();

				parts.RemoveRange(parts.Count - 4, 4);

				parts.Add(Path.Combine(gameAreaName, "bin", debug ? "Debug" : "Release"));

				path = string.Join(Path.DirectorySeparatorChar.ToString(), parts);

				path = Path.Combine(path, gameAreaName + ".dll");
			}

			var assembly = Assembly.LoadFile(path);
			return assembly;
		}

	}

	public sealed class GameFactory : MarshalByRefObject, IGameFactory
	{
		public IGame CreateGame(string gameAreaName, string gameDir)
		{
			return new Game(gameAreaName, gameDir);
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
