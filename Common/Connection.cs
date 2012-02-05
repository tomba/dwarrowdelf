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

		event Action<string> ConnectEvent;
		event Action DisconnectEvent;
		event Action<Message> ReceiveEvent;

		void BeginConnect();
		void BeginRead();
		void Send(Message msg);
		void Disconnect();
	}

	public sealed class Connection : IConnection
	{
		Socket m_socket;
		SendStream m_sendStream;

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		public bool IsConnected { get { return m_socket != null && m_socket.Connected; } }

		public event Action<string> ConnectEvent;
		public event Action DisconnectEvent;
		public event Action<Message> ReceiveEvent;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		object m_lock = new object();

		const int PORT = 9999;

		const uint MAGIC = 0x12345678;

		Thread m_deserializerThread;

		enum State
		{
			Uninitialized,
			Connecting,
			Connected,
			Receiving,
			Disconnected,
		}

		State m_state;

		public Connection()
		{
			m_state = State.Uninitialized;
		}

		public Connection(Socket client)
		{
			trace.Header = client.RemoteEndPoint.ToString();

			trace.TraceInformation("New Connection");

			if (client.Connected == false)
				throw new Exception();

			m_socket = client;
			m_sendStream = new SendStream(client);

			m_state = State.Connected;
		}

		/// <summary>
		/// m_lock has to be held when calling this
		/// </summary>
		void Cleanup()
		{
			if (m_socket != null)
			{
				m_socket.Shutdown(SocketShutdown.Both);
				m_socket.Close();
			}

			m_socket = null;

			m_state = State.Disconnected;
		}

		public void BeginConnect()
		{
			lock (m_lock)
			{
				if (m_state != State.Uninitialized)
					throw new Exception();

				Debug.Assert(m_socket == null);

				m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				var localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
				m_socket.Bind(localEndPoint);

				var port = PORT;

				var remoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);

				trace.Header = m_socket.LocalEndPoint.ToString();
				trace.TraceInformation("BeginConnect to {0}", remoteEndPoint);

				m_state = State.Connecting;

				try
				{
					m_socket.BeginConnect(remoteEndPoint, ConnectCallback, m_socket);
				}
				catch
				{
					Cleanup();
					throw;
				}
			}
		}

		void ConnectCallback(IAsyncResult ar)
		{
			trace.TraceInformation("ConnectCallback");

			var socket = (Socket)ar.AsyncState;

			string err = null;

			try
			{
				lock (m_lock)
				{
					socket.EndConnect(ar);

					m_state = State.Connected;

					m_sendStream = new SendStream(socket);
				}
			}
			catch (Exception e)
			{
				lock (m_lock)
					Cleanup();

				trace.TraceWarning("Connect failed: {0}", e.Message);

				err = e.Message;
			}

			if (this.ConnectEvent != null)
				this.ConnectEvent(err);
		}

		public void BeginRead()
		{
			lock (m_lock)
			{
				if (m_state != State.Connected)
					throw new Exception();

				Debug.Assert(m_socket != null);

				m_state = State.Receiving;

				var recvStream = new RecvStream(m_socket, 16, trace);

				if (m_deserializerThread != null)
					throw new Exception();
				m_deserializerThread = new Thread(DeserializerMain);

				m_deserializerThread.Start(recvStream);
			}
		}

		void DeserializerMain(object data)
		{
			RecvStream recvStream = (RecvStream)data;

			while (true)
			{
				uint len = recvStream.ReadBytes;

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
				this.ReceivedBytes += (int)len;

				if (ReceiveEvent != null)
					ReceiveEvent(msg);
			}
		}

		public void Disconnect()
		{
			trace.TraceInformation("Disconnect");

			lock (m_lock)
			{
				if (m_state == State.Uninitialized)
					return;

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

			Socket socket;

			lock (m_lock)
			{
				if (m_state != State.Receiving)
					throw new Exception();

				socket = m_socket;
			}

			lock (m_sendStream)
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
				/*
				SocketError error;
				int sent = socket.Send(m_sendBuffer, 0, len, SocketFlags.None, out error);

				if (sent != len)
				{
					trace.TraceError("[TX]: Short send {0} != {1}", sent, len);
					Cleanup();
					return;
				}
				else if (error != SocketError.Success)
				{
					trace.TraceError("[TX]: error {0}", error);
					Cleanup();
					return;
				}
				*/
				this.SentMessages++;
				this.SentBytes += len;
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
			uint m_lenMask;

			volatile uint m_received;
			volatile uint m_read;

			ArraySegment<byte>[] m_segments = new ArraySegment<byte>[2];

			Socket m_socket;
			Thread m_receiveThread;

			AutoResetEvent m_receiveEvent = new AutoResetEvent(false);
			AutoResetEvent m_readEvent = new AutoResetEvent(false);

			MyTraceSource trace;

			public RecvStream(Socket socket, int capacity, MyTraceSource traceSource)
			{
				m_socket = socket;

				int len = 1 << capacity;
				m_buffer = new byte[len];
				m_lenMask = (uint)len - 1;

				trace = traceSource;

				m_receiveThread = new Thread(ReceiveMain);
				m_receiveThread.Name = "Receive";
				m_receiveThread.Start();
			}

			public uint ReceivedBytes { get { return m_received; } }
			public uint ReadBytes { get { return m_read; } }

			void ReceiveMain()
			{
				uint received = 0;

				while (true)
				{
					// Stall if rx buffer full

					while (received - m_read == m_buffer.Length)
					{
						trace.TraceVerbose("rx buf full, stall");
						m_readEvent.WaitOne();
					}

					// create array segments

					uint consumed = m_read;

					var used = received - consumed;

					var tail = (int)(received & m_lenMask);
					var head = (int)(consumed & m_lenMask);

					int count;

					if (tail >= head)
						count = m_buffer.Length - tail;
					else
						count = head - tail;

					m_segments[0] = new ArraySegment<byte>(m_buffer, tail, count);

					if (tail >= head)
						count = head;
					else
						count = 0;

					m_segments[1] = new ArraySegment<byte>(m_buffer, 0, count);

					//Debug.WriteLine("ARRAYSEG {0}/{1}, {2}/{3}", m_segments[0].Offset, m_segments[0].Count, m_segments[1].Offset, m_segments[1].Count);

					// receive

					int len = m_socket.Receive(m_segments);

					trace.TraceVerbose("[RX R] Receive() = {0}", len);

					if (len == 0)
					{
						trace.TraceInformation("ReadCallback: empty read");

						//if (DisconnectEvent != null)
						//	DisconnectEvent();

						return;
					}

					received += (uint)len;
					m_received = received;

					m_receiveEvent.Set();
				}
			}

			public override int ReadByte()
			{
				uint read = m_read;

				while (m_received - read == 0)
				{
					trace.TraceVerbose("rx buf empty, stall");
					m_receiveEvent.WaitOne();
				}

				var b = m_buffer[read & m_lenMask];
				m_read = read + 1;

				m_readEvent.Set();

				return b;
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

			public SendStream(Socket socket)
			{
				m_socket = socket;
				m_buffer = new byte[4096];
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
