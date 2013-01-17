using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public static class TcpConnectionListener
	{
		static Socket s_listenSocket;
		static ManualResetEvent s_acceptStopEvent;
		volatile static bool s_stopListen;
		static MyTraceSource s_trace = new MyTraceSource("Dwarrowdelf.Connection", "Connection");

		static Action<TcpConnection> s_callback;

		public static void StartListening(Action<TcpConnection> callback)
		{
			s_trace.TraceInformation("StartListening");

			s_callback = callback;

			int port = TcpConnection.PORT;

			if (s_listenSocket != null)
				throw new Exception();

			s_acceptStopEvent = new ManualResetEvent(false);
			s_stopListen = false;

			var ep = new IPEndPoint(IPAddress.Loopback, port);
			s_listenSocket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			s_listenSocket.Bind(ep);
			s_listenSocket.Listen(100);
			var ar = s_listenSocket.BeginAccept(AcceptCallback, s_listenSocket);
			if (ar.CompletedSynchronously == true)
				throw new Exception();
		}

		public static void StopListening()
		{
			s_trace.TraceInformation("StopListening");

			if (s_listenSocket == null)
				throw new Exception();

			s_stopListen = true;
			s_listenSocket.Close();

			s_acceptStopEvent.WaitOne();

			s_acceptStopEvent.Close();
			s_acceptStopEvent = null;

			s_listenSocket = null;
		}

		static void AcceptCallback(IAsyncResult ar)
		{
			s_trace.TraceInformation("AcceptCallback");

			var listenSocket = (Socket)ar.AsyncState;

			if (s_stopListen)
			{
				s_acceptStopEvent.Set();
				return;
			}

			var socket = listenSocket.EndAccept(ar);

			var conn = new TcpConnection(socket);
			s_callback(conn);

			ar = s_listenSocket.BeginAccept(AcceptCallback, listenSocket);
			if (ar.CompletedSynchronously == true)
				throw new Exception();
		}
	}
}
