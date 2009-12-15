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
		MyDebugListener m_traceListener;

		public Server()
		{
			MyDebug.DefaultFlags = DebugFlag.Server;
		}

		public MyDebugListener TraceListener 
		{ 
			set 
			{
				if (value != null)
				{
					Debug.Assert(m_traceListener == null);
					m_traceListener = value;
					MyDebug.Listener = value;
				}
				else
				{
					if (m_traceListener != null)
						MyDebug.Listener = null;
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
			world.Start();
			World.TheWorld = world;

			var listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9999));
			listener.Start();
			listener.BeginAcceptTcpClient(AcceptTcpClientCallback, listener);

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

			m_acceptStopEvent = new ManualResetEvent(false);

			listener.Stop();

			m_acceptStopEvent.WaitOne();
			m_acceptStopEvent.Close();
			m_acceptStopEvent = null;

			MyDebug.WriteLine("Server exit");
		}

		ManualResetEvent m_acceptStopEvent;

		public void AcceptTcpClientCallback(IAsyncResult ar)
		{
			TcpListener listener = (TcpListener)ar.AsyncState;
			TcpClient client;

			if (!listener.Server.IsBound)
			{
				m_acceptStopEvent.Set();
				return;
			}

			client = listener.EndAcceptTcpClient(ar);

			new ServerConnection(client, World.TheWorld);

			listener.BeginAcceptTcpClient(AcceptTcpClientCallback, listener);
		}


		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MyDebug.WriteLine("tuli exc");

		}

	}
}
