using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MyGame.Messages;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;

namespace MyGame
{
	public class Connection
	{
		Socket m_socket;
		RecvStream m_recvStream = new RecvStream(1024 * 128);
		int m_expectedLen;

		byte[] m_sendBuffer = new byte[1024 * 128];

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		public bool IsConnected { get { return m_socket != null && m_socket.Connected; } }

		public event Action DisconnectEvent;
		public event Action<Message> ReceiveEvent;

		public Connection()
		{
		}

		public Connection(Socket client)
		{
			if (client.Connected == false)
				throw new Exception();

			m_socket = client;

			BeginRead();
		}

		public void BeginConnect(Action callback)
		{
			int port = 9999;

			if (m_socket != null)
				throw new Exception();

			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			m_socket.BeginConnect(IPAddress.Loopback, port, ConnectCallback, callback);
		}

		void ConnectCallback(IAsyncResult ar)
		{
			var callback = (Action)ar.AsyncState;
			m_socket.EndConnect(ar);

			callback.Invoke();

			BeginRead();
		}

		void BeginRead()
		{
			if (m_socket == null || !m_socket.Connected)
			{
				MyDebug.WriteLine("Socket not connected");
				return;
			}

			m_socket.BeginReceive(m_recvStream.ArraySegmentList, SocketFlags.None, ReadCallback, m_socket);
		}

		void ReadCallback(IAsyncResult ar)
		{
			if (m_socket == null || !m_socket.Connected)
			{
				MyDebug.WriteLine("Socket not connected");
				if (DisconnectEvent != null)
					DisconnectEvent();
				return;
			}

			var socket = (Socket)ar.AsyncState;
			int len = socket.EndReceive(ar);

			m_recvStream.WriteNotify(len);

			//MyDebug.WriteLine("[RX] {0} bytes", len);

			if (len == 0)
			{
				MyDebug.WriteLine("socket disconnected");
				if (DisconnectEvent != null)
					DisconnectEvent();
				return;
			}

			if (m_recvStream.UsedBytes < 8)
			{
				BeginRead();
				return;
			}

			while (m_recvStream.UsedBytes > 8)
			{
				if (m_expectedLen == 0)
				{
					using (var reader = new BinaryReader(m_recvStream))
					{
						var magic = reader.ReadInt32();

						if (magic != 0x12345678)
							throw new Exception();

						m_expectedLen = reader.ReadInt32() - 8;
					}

					//MyDebug.WriteLine("[RX] Expecting msg of {0} bytes", m_expectedLen);

					if (m_recvStream.UsedBytes < m_expectedLen && m_expectedLen > m_recvStream.FreeBytes)
						throw new Exception("message bigger than the receive buffer");
				}

				if (m_recvStream.UsedBytes >= m_expectedLen)
				{
					Message msg;

					msg = Serializer.Deserialize(m_recvStream);

					this.ReceivedMessages++;
					this.ReceivedBytes += m_expectedLen + 8;

					//MyDebug.WriteLine("[RX] {0} bytes, {1}", m_expectedLen, msg);
					if (ReceiveEvent != null)
						ReceiveEvent(msg);

					m_expectedLen = 0;
				}
				else
				{
					//MyDebug.WriteLine("[RX] {0} != {1}", m_expectedLen, m_recvStream.UsedBytes);
					break;
				}
			}

			BeginRead();
		}

		public void Disconnect()
		{
			m_socket.Shutdown(SocketShutdown.Both);
			m_socket.Close();
			m_socket = null;
		}

		public void Send(Message msg)
		{
			//MyDebug.WriteLine("[TX] {0}", msg);

			int len;

			lock (m_sendBuffer)
			{
				using (var stream = new MemoryStream(m_sendBuffer))
				{
					// Write the object starting at byte 8
					stream.Seek(8, SeekOrigin.Begin);
					Serializer.Serialize(stream, msg);
					len = (int)stream.Position;
					//MyDebug.WriteLine("[TX] sending {0} bytes", len);

					// Prepend the object data with magic and object len
					stream.Seek(0, SeekOrigin.Begin);
					using (var bw = new BinaryWriter(stream))
					{
						bw.Write((int)0x12345678);
						bw.Write(len);
					}
				}

				m_socket.Send(m_sendBuffer, 0, len, SocketFlags.None);

				this.SentMessages++;
				this.SentBytes += len;
			}
		}

		public static event Action<Connection> NewConnectionEvent;
		static Socket s_listenSocket;
		static ManualResetEvent s_acceptStopEvent;
		volatile static bool s_stopListen;

		public static void StartListening()
		{
			int port = 9999;

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
