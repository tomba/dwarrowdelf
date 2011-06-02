//#define SAVE_EVERY_TURN

using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

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
		List<ServerConnection> m_connections = new List<ServerConnection>();

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

			m_game.World.HandleMessagesEvent += OnHandleMessages; // XXX

			Connection.NewConnectionEvent += OnNewConnection;
			Connection.StartListening();

			Debug.Print("The server is ready.");

			Debug.Print("Server signaling client for start.");
			if (serverStartWaitHandle != null)
			{
				serverStartWaitHandle.Set();
				serverStopWaitHandle.WaitOne();
			}

			m_game.World.HandleMessagesEvent -= OnHandleMessages; // XXX

			m_game.Save();

			m_game.Stop();

			Debug.Print("Server exiting");

			Connection.StopListening();

			Debug.Print("Server exit");
		}

		void OnNewConnection(IConnection connection)
		{
			var serverConnection = new ServerConnection(m_game, connection);

			lock (m_connections)
				m_connections.Add(serverConnection);

			serverConnection.Start();

			// XXX remove connection from list
		}

		void OnHandleMessages()
		{
			List<ServerConnection> removeList = null;

			lock (m_connections)
			{
				foreach (var c in m_connections)
				{
					if (c.IsConnected)
						c.HandleNewMessages();
					else
					{
						if (removeList == null)
							removeList = new List<ServerConnection>();
						removeList.Add(c);
					}
				}

				if (removeList != null)
				{
					foreach (var c in removeList)
						m_connections.Remove(c);
				}
			}
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Debug.Print("tuli exc");
		}
	}
}
