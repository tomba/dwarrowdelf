using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Dwarrowdelf.Messages;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public interface IConnection
	{
		int SentMessages { get; }
		int SentBytes { get; }
		int ReceivedMessages { get; }
		int ReceivedBytes { get; }

		bool IsConnected { get; }

		void Start(Action<Message> receiveCallback, Action disconnectCallback);
		void Send(Message msg);
		void Disconnect();
	}

	public sealed class Connection : IConnection
	{
		Socket m_socket;
		SendStream m_sendStream;
		RecvStream m_recvStream;

		public bool IsConnected { get { lock (m_lock) { return m_socket != null && m_socket.Connected; } } }

		Action<Message> m_receiveCallback;
		Action m_disconnectCallback;

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		object m_lock = new object();

		public const int PORT = 9999;

		const uint MAGIC = 0x12345678;

		const int RECV_BUF_SIZEEXP = 16;
		const int SEND_BUF_SIZEEXP = 16;

		Thread m_deserializerThread;

		enum State
		{
			Connected,
			Operational,
			Disconnected,
		}

		State m_state;

		public Connection(Socket socket)
		{
			trace.Header = socket.RemoteEndPoint.ToString();

			trace.TraceInformation("New Connection");

			if (socket.Connected == false)
				throw new Exception();

			m_socket = socket;
			m_sendStream = new SendStream(socket, SEND_BUF_SIZEEXP);
			m_recvStream = new RecvStream(socket, RECV_BUF_SIZEEXP, trace);

			m_state = State.Connected;
		}

		/// <summary>
		/// m_lock has to be held when calling this
		/// </summary>
		void Cleanup()
		{
			trace.TraceVerbose("Cleanup");

			if (m_socket != null)
			{
				m_socket.Shutdown(SocketShutdown.Both);
				m_socket.Close();
			}

			m_socket = null;

			m_state = State.Disconnected;
		}

		public void Start(Action<Message> receiveCallback, Action disconnectCallback)
		{
			lock (m_lock)
			{
				if (m_state != State.Connected)
					throw new Exception();

				Debug.Assert(m_socket != null);
				Debug.Assert(m_deserializerThread == null);

				m_receiveCallback = receiveCallback;
				m_disconnectCallback = disconnectCallback;

				m_state = State.Operational;

				m_deserializerThread = new Thread(DeserializerMain);
				m_deserializerThread.Start();
			}
		}

		void DeserializerMain()
		{
			try
			{
				while (true)
				{
					var msg = Receive();

					m_receiveCallback(msg);
				}
			}
			catch (SocketException)
			{
				lock (m_lock)
				{
					m_disconnectCallback();

					Cleanup();
				}
			}

			trace.TraceVerbose("Deserializer thread ending");
		}

		Message Receive()
		{
			var recvStream = m_recvStream;

			int len = recvStream.ReadBytes;

			uint magic =
				(uint)recvStream.ReadByte() |
				(uint)recvStream.ReadByte() << 8 |
				(uint)recvStream.ReadByte() << 16 |
				(uint)recvStream.ReadByte() << 24;

			if (magic != MAGIC)
				throw new Exception();

			trace.TraceVerbose("[RX D] Deserializing");

			var msg = Serializer.Deserialize(recvStream);

			len = recvStream.ReadBytes - len;

			trace.TraceVerbose("[RX D] Deserialized {0} bytes, {1}", len, msg.GetType().Name);

			this.ReceivedMessages++;
			this.ReceivedBytes += len;

			return msg;
		}

		public void Disconnect()
		{
			trace.TraceInformation("Disconnect");

			lock (m_lock)
			{
				if (m_state == State.Disconnected)
					return;

				m_socket.Shutdown(SocketShutdown.Both);
			}

			SpinWait.SpinUntil(delegate { Thread.MemoryBarrier(); return m_state == State.Disconnected; });

			trace.TraceInformation("Disconnect done");
		}

		public void Send(Message msg)
		{
			trace.TraceVerbose("[TX] sending {0}", msg.GetType().Name);

			if (this.IsConnected == false)
				throw new Exception();

			lock (m_sendStream)
			{
				try
				{
					var stream = m_sendStream;

					int len = stream.SentBytes;

					stream.WriteByte((byte)((MAGIC >> 0) & 0xff));
					stream.WriteByte((byte)((MAGIC >> 8) & 0xff));
					stream.WriteByte((byte)((MAGIC >> 16) & 0xff));
					stream.WriteByte((byte)((MAGIC >> 24) & 0xff));

					Serializer.Serialize(stream, msg);

					stream.Flush();

					len = stream.SentBytes - len;

					trace.TraceVerbose("[TX] sent {0} bytes", len);

					this.SentMessages++;
					this.SentBytes += len;
				}
				catch (SocketException e)
				{
					var error = e.SocketErrorCode;

					trace.TraceError("[TX]: socket error {0}", error);

					lock (m_lock)
						Cleanup();
				}
			}
		}

		public static event Action<Connection> NewConnectionEvent;
		static Socket s_listenSocket;
		static ManualResetEvent s_acceptStopEvent;
		volatile static bool s_stopListen;
		static MyTraceSource s_trace = new MyTraceSource("Dwarrowdelf.Connection", "Connection");

		public static void StartListening()
		{
			s_trace.TraceInformation("StartListening");

			int port = PORT;

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

			var conn = new Connection(socket);
			if (NewConnectionEvent != null)
				NewConnectionEvent(conn);

			ar = s_listenSocket.BeginAccept(AcceptCallback, listenSocket);
			if (ar.CompletedSynchronously == true)
				throw new Exception();
		}

		sealed class RecvStream : Stream
		{
			byte[] m_buffer;

			int m_received;
			int m_read;
			int m_totalRead;

			Socket m_socket;

			MyTraceSource trace;

			public RecvStream(Socket socket, int capacity, MyTraceSource traceSource)
			{
				m_socket = socket;

				int len = 1 << capacity;
				m_buffer = new byte[len];

				trace = traceSource;
			}

			public int ReadBytes { get { return m_totalRead; } }

			public override int ReadByte()
			{
				if (m_received == m_read)
				{
					int len = m_socket.Receive(m_buffer);

					if (len == 0)
					{
						trace.TraceInformation("Receive: socket closed, throwing exception");
						throw new SocketException();
					}

					m_received = len;
					m_read = 0;
				}

				m_totalRead++;
				return m_buffer[m_read++];
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override bool CanRead { get { return true; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return false; } }

			public override void Flush()
			{
				throw new NotImplementedException();
			}

			public override long Length
			{
				get { return m_buffer.LongLength; }
			}

			public override long Position
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override void WriteByte(byte value)
			{
				throw new NotImplementedException();
			}
		}

		sealed class SendStream : Stream
		{
			byte[] m_buffer;
			int m_used;
			Socket m_socket;
			int m_sent;

			public SendStream(Socket socket, int capacity)
			{
				int len = 1 << capacity;
				m_socket = socket;
				m_buffer = new byte[len];
			}

			public int SentBytes { get { return m_sent; } }

			public override bool CanRead { get { return false; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return true; } }

			public override void Flush()
			{
				int len = m_socket.Send(m_buffer, m_used, SocketFlags.None);
				if (len != m_used)
					throw new Exception("short write");

				m_sent += len;

				m_used = 0;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override void WriteByte(byte value)
			{
				if (m_used == m_buffer.Length)
					Flush();

				m_buffer[m_used++] = value;
			}

			public override long Length
			{
				get { return m_buffer.LongLength; }
			}

			public override long Position
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}
		}
	}
}
