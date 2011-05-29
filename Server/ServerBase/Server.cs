//#define SAVE_EVERY_TURN

using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Dwarrowdelf.Server
{
	public class ServerFactory : MarshalByRefObject, IServerFactory
	{
		public IServer CreateGameAndServer(string gameDll, string gameDir)
		{
			var basePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

			var path = Path.Combine(basePath, gameDll);

			var assembly = Assembly.LoadFile(path);
			var game = (Game)assembly.CreateInstance("MyArea.MyGame", false, BindingFlags.Public | BindingFlags.Instance, null, new object[] { gameDir }, null, null);

			var server = new Server(game);

			return server;
		}
	}

	public class Server : MarshalByRefObject, IServer
	{
		Game m_game;

		public Server(Game game)
		{
			m_game = game;
		}

		public void RunServer(EventWaitHandle serverStartWaitHandle, EventWaitHandle serverStopWaitHandle)
		{
			Debug.Print("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			int magic = 0;
			GameAction.MagicNumberGenerator = () => -Math.Abs(Interlocked.Increment(ref magic));

			m_game.Start();

			Connection.NewConnectionEvent += OnNewConnection;
			Connection.StartListening();

			Debug.Print("The server is ready.");

			Debug.Print("Server signaling client for start.");
			if (serverStartWaitHandle != null)
			{
				serverStartWaitHandle.Set();
				serverStopWaitHandle.WaitOne();
			}

			m_game.Save();

			m_game.Stop();

			Debug.Print("Server exiting");

			Connection.StopListening();

			Debug.Print("Server exit");
		}

		void OnNewConnection(IConnection connection)
		{
			var serverConnection = new ServerConnection(m_game, connection);
			m_game.AddNewConnection(serverConnection);
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Debug.Print("tuli exc");
		}
	}
}
