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

	public class Connection : IConnection
	{
		Socket m_socket;

		byte[] m_sendBuffer = new byte[1024 * 128];

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

		class ReadState
		{
			public Socket Socket;
			public RecvStream RecvStream;
			public int ExpectedLen;
		}

		public void BeginRead()
		{
			lock (m_lock)
			{
				if (m_state != State.Connected)
					throw new Exception();

				Debug.Assert(m_socket != null);

				m_state = State.Receiving;

				using (var recvStream = new RecvStream(1024 * 128))
				{

					var state = new ReadState()
					{
						Socket = m_socket,
						RecvStream = recvStream,
					};

					m_socket.BeginReceive(recvStream.ArraySegmentList, SocketFlags.None, ReadCallback, state);
				}
			}
		}

		void ReadCallback(IAsyncResult ar)
		{
			try
			{
				DoRead(ar);
			}
			catch (Exception e)
			{
				lock (m_lock)
					Cleanup();

				trace.TraceWarning("ReadCallback: {0}", e.Message);

				if (DisconnectEvent != null)
					DisconnectEvent();
			}
		}

		void DoRead(IAsyncResult ar)
		{
			var state = (ReadState)ar.AsyncState;

			var socket = state.Socket;
			var recvStream = state.RecvStream;

			int len = socket.EndReceive(ar);

			if (len == 0)
			{
				lock (m_lock)
					Cleanup();

				trace.TraceWarning("ReadCallback: empty read");

				if (DisconnectEvent != null)
					DisconnectEvent();

				return;
			}

			trace.TraceVerbose("[RX] {0} bytes", len);

			recvStream.WriteNotify(len);

			if (recvStream.UsedBytes < 8)
			{
				socket.BeginReceive(recvStream.ArraySegmentList, SocketFlags.None, ReadCallback, state);
				return;
			}

			while (recvStream.UsedBytes > 8)
			{
				if (state.ExpectedLen == 0)
				{
					using (var reader = new BinaryReader(recvStream))
					{
						var magic = reader.ReadInt32();

						if (magic != 0x12345678)
							throw new Exception();

						state.ExpectedLen = reader.ReadInt32() - 8;
					}

					trace.TraceVerbose("[RX] Expecting msg of {0} bytes", state.ExpectedLen);

					if (recvStream.UsedBytes < state.ExpectedLen && state.ExpectedLen > recvStream.FreeBytes)
						throw new Exception("message bigger than the receive buffer");
				}

				if (recvStream.UsedBytes >= state.ExpectedLen)
				{
					Message msg;

					msg = Serializer.Deserialize(recvStream);

					this.ReceivedMessages++;
					this.ReceivedBytes += state.ExpectedLen + 8;

					trace.TraceVerbose("[RX] {0} bytes, {1}", state.ExpectedLen, msg);
					if (ReceiveEvent != null)
						ReceiveEvent(msg);

					state.ExpectedLen = 0;
				}
				else
				{
					trace.TraceVerbose("[RX] {0} != {1}", state.ExpectedLen, recvStream.UsedBytes);
					break;
				}
			}

			socket.BeginReceive(recvStream.ArraySegmentList, SocketFlags.None, ReadCallback, state);
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
			trace.TraceVerbose("[TX] {0}", msg);

			int len;

			Socket socket;

			lock (m_lock)
			{
				if (m_state != State.Receiving)
					throw new Exception();

				socket = m_socket;
			}

			lock (m_sendBuffer)
			{
				using (var stream = new MemoryStream(m_sendBuffer))
				{
					// Write the object starting at byte 8
					stream.Seek(8, SeekOrigin.Begin);
					Serializer.Serialize(stream, msg);
					len = (int)stream.Position;

					// Prepend the object data with magic and object len
					stream.Seek(0, SeekOrigin.Begin);
					using (var bw = new BinaryWriter(stream))
					{
						bw.Write((int)0x12345678);
						bw.Write(len);
					}
				}

				trace.TraceVerbose("[TX] sending {0} bytes", len);
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

		class RecvStream : Stream
		{
			byte[] m_buffer;
			ArraySegment<byte>[] m_segments = new ArraySegment<byte>[2];

			int m_head;
			int m_tail;
			int m_used;

			public RecvStream(int capacity)
			{
				m_buffer = new byte[capacity];
			}

			public int UsedBytes { get { return m_used; } }
			public int FreeBytes { get { return m_buffer.Length - m_used; } }

			public void WriteNotify(int count)
			{
				if (count > m_buffer.Length - m_used)
					throw new Exception();

				m_tail = (m_tail + count) % m_buffer.Length;
				m_used += count;
			}

			public void ReadNotify(int count)
			{
				if (count > m_used)
					throw new Exception();

				m_head = (m_head + count) % m_buffer.Length;
				m_used -= count;
			}

			public IList<ArraySegment<byte>> ArraySegmentList
			{
				get
				{
					if (m_used == m_buffer.Length)
						throw new Exception();

					int count;

					if (m_tail >= m_head)
						count = m_buffer.Length - m_tail;
					else
						count = m_head - m_tail;

					m_segments[0] = new ArraySegment<byte>(m_buffer, m_tail, count);

					if (m_tail >= m_head)
						count = m_head;
					else
						count = 0;

					m_segments[1] = new ArraySegment<byte>(m_buffer, 0, count);

					//MyDebug.WriteLine("ARRAYSEG {0}/{1}, {2}/{3}", m_segments[0].Offset, m_segments[0].Count, m_segments[1].Offset, m_segments[1].Count);

					return m_segments;
				}
			}

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanSeek
			{
				get { return false; }
			}

			public override bool CanWrite
			{
				get { return true; }
			}

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

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (count > m_used)
					throw new Exception();

				int c = count;

				int c1;
				if (m_tail > m_head)
					c1 = m_tail - m_head;
				else
					c1 = m_buffer.Length - m_head;

				c1 = Math.Min(c1, c);

				if (c1 > 0)
					Array.Copy(m_buffer, m_head, buffer, offset, c1);

				c -= c1;

				int c2;
				if (m_tail > m_head)
					c2 = 0;
				else
					c2 = m_tail;

				c2 = Math.Min(c2, c);

				if (c2 > 0)
					Array.Copy(m_buffer, 0, buffer, offset + c1, c2);

				//MyDebug.WriteLine("READ {0}/{1}, {2}/{3}", m_head, c1, 0, c2);

				m_head = (m_head + count) % m_buffer.Length;
				m_used -= count;

				if (m_used == 0)
					m_head = m_tail = 0;

				return count;
			}

			public override int ReadByte()
			{
				if (m_used == 0)
					return -1;

				var b = m_buffer[m_head];

				//MyDebug.WriteLine("READ {0}/{1} : {2:x2}", m_head, 1, b);

				m_head = (m_head + 1) % m_buffer.Length;
				--m_used;

				if (m_used == 0)
					m_head = m_tail = 0;

				return b;
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
	}
}
