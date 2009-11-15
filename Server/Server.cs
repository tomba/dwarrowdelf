#define LOCAL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

namespace MyGame
{
	public class Server : MarshalByRefObject, IServer
	{
		TraceListener m_traceListener;

		public Server()
		{
			MyDebug.Prefix = "[Server] ";
		}

		public TraceListener TraceListener 
		{ 
			set 
			{
				if (value != null)
				{
					Debug.Assert(m_traceListener == null);
					m_traceListener = value;
					Debug.Listeners.Add(value);
				}
				else
				{
					if (m_traceListener != null)
						Debug.Listeners.Remove(m_traceListener);
					m_traceListener = null;
				}
			}
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
			World.TheWorld = world;

			var listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9999));
			listener.Start();
			var a = listener.BeginAcceptTcpClient(AcceptTcpClientCallback, listener);

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

			MyDebug.WriteLine("Server exiting");

			listener.Stop();

			MyDebug.WriteLine("Server exit");
		}

		public static void AcceptTcpClientCallback(IAsyncResult ar)
		{
			TcpListener listener = (TcpListener)ar.AsyncState;
			TcpClient client = listener.EndAcceptTcpClient(ar);

			var c = new ServerConnection(client);
			c.Start();
		}


		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MyDebug.WriteLine("tuli exc");

		}

	}
}
