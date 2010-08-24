using System;
using System.Threading;

namespace MyGame.Server
{
	public class Server : MarshalByRefObject, IServer
	{
		World m_world;

		public Server()
		{
			MyDebug.Component = "Server";
		}

		public void RunServer(bool isEmbedded,
			EventWaitHandle serverStartWaitHandle, EventWaitHandle serverStopWaitHandle)
		{
			MyDebug.WriteLine("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			/* Load area */

			IArea area = new MyArea.Area();

			m_world = new World(area);
			m_world.Start();

			Connection.NewConnectionEvent += OnNewConnection;
			Connection.StartListening();

			MyDebug.WriteLine("The service is ready.");

			if (isEmbedded)
			{
				MyDebug.WriteLine("Server signaling client for start.");
				if (serverStartWaitHandle != null)
				{
					serverStartWaitHandle.Set();
					serverStopWaitHandle.WaitOne();
				}
			}
			else
			{
				Console.WriteLine("Press enter to exit");
				while (Console.ReadKey().Key != ConsoleKey.Enter)
					m_world.SignalWorld();
			}

			m_world.Stop();

			MyDebug.WriteLine("Server exiting");

			Connection.StopListening();

			MyDebug.WriteLine("Server exit");
		}


		void OnNewConnection(Connection conn)
		{
			new ServerConnection(conn, m_world);
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MyDebug.WriteLine("tuli exc");
		}

	}
}
