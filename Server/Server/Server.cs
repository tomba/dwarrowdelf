﻿using System;
using System.Threading;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public class Server : MarshalByRefObject, IServer
	{
		World m_world;

		public Server()
		{
		}

		public void RunServer(bool isEmbedded, EventWaitHandle serverStartWaitHandle, EventWaitHandle serverStopWaitHandle)
		{
			Debug.Print("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			/* Load area */

			int magic = 0;
			GameAction.MagicNumberGenerator = () => -Math.Abs(Interlocked.Increment(ref magic));

			IArea area = new MyArea.Area();

			m_world = new World(area);
			m_world.Start();

			Connection.NewConnectionEvent += OnNewConnection;
			Connection.StartListening();

			Debug.Print("The service is ready.");

			if (isEmbedded)
			{
				Debug.Print("Server signaling client for start.");
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

			Debug.Print("Server exiting");

			Connection.StopListening();

			Debug.Print("Server exit");
		}


		void OnNewConnection(IConnection conn)
		{
			new ServerConnection(conn, m_world);
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Debug.Print("tuli exc");
		}

	}
}
