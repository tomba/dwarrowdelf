using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MyGame.ClientMsgs;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;

namespace MyGame
{
	public class Connection
	{
		TcpClient m_client;
		NetworkStream m_netStream;
		byte[] m_recvBuffer = new byte[1024 * 1024];
		int m_bufferUsed;
		int m_expectedLen;

		byte[] m_sendBuffer = new byte[1024 * 1024];

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		public bool IsConnected { get { return m_client != null && m_client.Connected; } }

		public event Action DisconnectEvent;
		public event Action<Message> ReceiveEvent;

		public Connection()
		{
		}

		public Connection(TcpClient client)
		{
			if (client.Connected == false)
				throw new Exception();

			m_client = client;
			m_netStream = m_client.GetStream();

			BeginRead();
		}

		public void BeginConnect(Action callback)
		{
			if (m_client != null)
				throw new Exception();

			m_client = new TcpClient();

			m_client.BeginConnect(IPAddress.Loopback, 9999, ConnectCallback, callback);
		}

		void ConnectCallback(IAsyncResult ar)
		{
			var callback = (Action)ar.AsyncState;
			m_client.EndConnect(ar);

			m_netStream = m_client.GetStream();

			callback.Invoke();

			BeginRead();
		}

		void BeginRead()
		{
			m_netStream.BeginRead(m_recvBuffer, m_bufferUsed, m_recvBuffer.Length - m_bufferUsed, ReadCallback, m_netStream);
		}

		void ReadCallback(IAsyncResult ar)
		{
			if (m_client == null || !m_client.Client.Connected)
			{
				MyDebug.WriteLine("Socket not connected");
				if (DisconnectEvent != null)
					DisconnectEvent();
				return;
			}

			var stream = (NetworkStream)ar.AsyncState;
			int len = stream.EndRead(ar);

			//MyDebug.WriteLine("[RX] {0} bytes", len);

			if (len == 0)
			{
				MyDebug.WriteLine("socket disconnected");
				if (DisconnectEvent != null)
					DisconnectEvent();
				return;
			}

			m_bufferUsed += len;

			if (len < 8)
			{
				BeginRead();
				return;
			}

			while (m_bufferUsed > 8)
			{
				if (m_expectedLen == 0)
				{
					using (var memstream = new MemoryStream(m_recvBuffer, 0, len))
					{
						using (var reader = new BinaryReader(memstream))
						{
							var magic = reader.ReadInt32();

							if (magic != 0x12345678)
								throw new Exception();

							m_expectedLen = reader.ReadInt32();
						}
					}

					//MyDebug.WriteLine("[RX] Expecting msg of {0} bytes", m_expectedLen);

					if (m_expectedLen > m_recvBuffer.Length)
						throw new Exception();
				}

				if (m_bufferUsed >= m_expectedLen)
				{
					Message msg;

					using (var memstream = new MemoryStream(m_recvBuffer, 8, m_expectedLen - 8))
						msg = Serializer.Deserialize(memstream);

					//MyDebug.WriteLine("[RX] {0} bytes, {1}", m_expectedLen, msg);
					if (ReceiveEvent != null)
						ReceiveEvent(msg);

					this.ReceivedMessages++;
					this.ReceivedBytes += m_expectedLen;

					int copy = m_bufferUsed - m_expectedLen;
					Array.Copy(m_recvBuffer, m_expectedLen, m_recvBuffer, 0, copy);

					m_bufferUsed -= m_expectedLen;
					m_expectedLen = 0;
				}
				else
				{
					//MyDebug.WriteLine("[RX] {0} != {1}", m_expectedLen, m_bufferUsed);
					break;
				}
			}

			BeginRead();
		}

		public void Disconnect()
		{
			m_client.Client.Shutdown(SocketShutdown.Both);
			m_client.Close();
			m_netStream.Close();
			m_client = null;
		}

		public virtual void Send(Message msg)
		{
			MyDebug.WriteLine("[TX] {0}", msg);

			int len;

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

			m_netStream.Write(m_sendBuffer, 0, len);

			this.SentMessages++;
			this.SentBytes += len;
		}

		public static event Action<Connection> NewConnectionEvent;
		static TcpListener s_listener;
		static ManualResetEvent s_acceptStopEvent;

		public static void StartListening(int port)
		{
			if (s_listener != null)
				throw new Exception();

			s_acceptStopEvent = new ManualResetEvent(false);

			s_listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
			s_listener.Start();
			s_listener.BeginAcceptTcpClient(AcceptTcpClientCallback, s_listener);
		}

		public static void StopListening()
		{
			if (s_listener == null)
				throw new Exception();

			s_listener.Stop();

			s_acceptStopEvent.WaitOne();

			s_acceptStopEvent.Close();
			s_acceptStopEvent = null;

			s_listener = null;
		}

		static void AcceptTcpClientCallback(IAsyncResult ar)
		{
			TcpListener listener = (TcpListener)ar.AsyncState;

			if (!listener.Server.IsBound)
			{
				s_acceptStopEvent.Set();
				return;
			}

			var client = listener.EndAcceptTcpClient(ar);
			var conn = new Connection(client);
			if (NewConnectionEvent != null)
				NewConnectionEvent(conn);

			listener.BeginAcceptTcpClient(AcceptTcpClientCallback, listener);
		}
	}
}
