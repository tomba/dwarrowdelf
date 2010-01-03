using System;
using System.Threading;

namespace MyGame.Server
{
	public class Server : MarshalByRefObject, IServer
	{
		public Server()
		{
			MyDebug.DefaultFlags = DebugFlag.Server;
		}

		public void RunServer(bool isEmbedded,
			EventWaitHandle serverStartWaitHandle, EventWaitHandle serverStopWaitHandle)
		{
			MyDebug.WriteLine("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			/* Load area */

			IAreaData areaData = new MyAreaData.AreaData();
			IArea area = new MyArea.Area();

			var world = new World(area, areaData);
			world.Start();
			World.TheWorld = world;

			Connection.NewConnectionEvent += OnNewConnection;
			Connection.StartListening(9999);

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
					world.SignalWorld();
			}

			world.Stop();

			MyDebug.WriteLine("Server exiting");

			Connection.StopListening();

			MyDebug.WriteLine("Server exit");
		}


		void OnNewConnection(Connection conn)
		{
			new ServerConnection(conn, World.TheWorld);
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MyDebug.WriteLine("tuli exc");
		}

	}
}
