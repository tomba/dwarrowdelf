//#define SAVE_EVERY_TURN

using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace Dwarrowdelf.Server
{
	public class GameServer
	{
		GameEngine m_engine;
		List<ServerConnection> m_connections = new List<ServerConnection>();

		public GameServer(GameEngine engine)
		{
			m_engine = engine;
		}

		public void Run(EventWaitHandle serverStartWaitHandle)
		{
			Debug.Print("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			int magic = 0;
			GameAction.MagicNumberGenerator = () => -Math.Abs(Interlocked.Increment(ref magic));

			m_engine.World.HandleMessagesEvent += OnHandleMessages; // XXX

			Connection.NewConnectionEvent += OnNewConnection;
			Connection.StartListening();

			Debug.Print("The server is ready.");

			serverStartWaitHandle.Set();

			m_engine.Run();

			m_engine.World.HandleMessagesEvent -= OnHandleMessages; // XXX

			m_engine.Save();

			Debug.Print("Server exiting");

			Connection.StopListening();

			Debug.Print("Server exit");
		}

		public void Stop()
		{
			m_engine.Stop();
		}

		void OnNewConnection(IConnection connection)
		{
			var serverConnection = new ServerConnection(m_engine, connection);

			lock (m_connections)
				m_connections.Add(serverConnection);

			serverConnection.Start();

			m_engine.SignalWorld();
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
			Debugger.Break();
		}
	}
}
