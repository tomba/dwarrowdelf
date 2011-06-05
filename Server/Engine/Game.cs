using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;

namespace Dwarrowdelf.Server
{
	class Game : MarshalByRefObject, IGame
	{
		GameServer m_server;
		GameEngine m_engine;

		public Game(GameServer server, GameEngine engine)
		{
			m_server = server;
			m_engine = engine;
		}

		public void Run(EventWaitHandle serverStartWaitHandle)
		{
			m_server.Run(serverStartWaitHandle);
		}

		public void Stop()
		{
			m_server.Stop();
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
