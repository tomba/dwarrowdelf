using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MyGame.ClientMsgs;
using System.Runtime.Serialization;
using System.IO;

namespace MyGame
{
	public abstract class Connection
	{
		TcpClient m_client;
		NetworkStream m_netStream;
		Serializer m_serializer = new Serializer();
		byte[] m_buffer = new byte[1024 * 1024];
		int m_bufferUsed;
		int m_expectedLen;

		protected TcpClient Client { get { return m_client; } }

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		public Connection()
		{
			m_client = new TcpClient();
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
			if (m_client.Connected == true)
				throw new Exception();

			Client.BeginConnect(IPAddress.Loopback, 9999, ConnectCallback, callback);
		}

		void ConnectCallback(IAsyncResult ar)
		{
			var callback = (Action)ar.AsyncState;
			Client.EndConnect(ar);

			m_netStream = m_client.GetStream();

			callback.Invoke();

			BeginRead();
		}

		void BeginRead()
		{
			m_netStream.BeginRead(m_buffer, m_bufferUsed, m_buffer.Length - m_bufferUsed, ReadCallback, m_netStream);
		}

		void ReadCallback(IAsyncResult ar)
		{
			if (!m_client.Client.Connected)
			{
				MyDebug.WriteLine("Socket not connected");
				return;
			}

			var stream = (NetworkStream)ar.AsyncState;
			int len = stream.EndRead(ar);

			//MyDebug.WriteLine("[RX] {0} bytes", len);

			if (len == 0)
			{
				MyDebug.WriteLine("socket disconnected");
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
					using (var memstream = new MemoryStream(m_buffer, 0, len))
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

					if (m_expectedLen > m_buffer.Length)
						throw new Exception();
				}

				if (m_bufferUsed >= m_expectedLen)
				{
					Message msg;

					using (var memstream = new MemoryStream(m_buffer, 8, m_expectedLen - 8))
						msg = m_serializer.Deserialize(memstream);

					//MyDebug.WriteLine("[RX] {0} bytes, {1}", m_expectedLen, msg);
					ReceiveMessage(msg);
					this.ReceivedMessages++;
					this.ReceivedBytes += m_expectedLen;

					int copy = m_bufferUsed - m_expectedLen;
					Array.Copy(m_buffer, m_expectedLen, m_buffer, 0, copy);

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

		protected abstract void ReceiveMessage(Message msg);

		public void Disconnect()
		{
			m_client.Client.Shutdown(SocketShutdown.Both);
			m_client.Close();
			m_netStream.Close();
		}

		public virtual void Send(Message msg)
		{
			MyDebug.WriteLine("[TX] {0}", msg);

			var bytes = m_serializer.Send(m_client.GetStream(), msg);
			this.SentMessages++;
			this.SentBytes += bytes;
		}
	}
}
