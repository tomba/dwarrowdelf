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
			m_client = client;
			BeginRead();
		}

		public void BeginConnect(Action callback)
		{
			Client.BeginConnect(IPAddress.Loopback, 9999, ConnectCallback, callback);
		}

		void ConnectCallback(IAsyncResult ar)
		{
			var callback = (Action)ar.AsyncState;
			Client.EndConnect(ar);

			callback.Invoke();

			BeginRead();
		}

		void BeginRead()
		{
			var stream = m_client.GetStream();
			stream.BeginRead(m_buffer, m_bufferUsed, m_buffer.Length - m_bufferUsed, ReadCallback, stream);
		}

		void ReadCallback(IAsyncResult ar)
		{
			if (!m_client.Client.Connected)
				return;

			var stream = (NetworkStream)ar.AsyncState;
			int len = stream.EndRead(ar);

			//MyDebug.WriteLine("Received {0} bytes", len);

			if (len == 0)
			{
				return;
			}

			m_bufferUsed += len;

			if (len < 4)
			{
				BeginRead();
				return;
			}

			while (m_bufferUsed > 0)
			{
				if (m_expectedLen == 0)
				{
					var memstream = new MemoryStream(m_buffer, 0, len);
					m_expectedLen = new BinaryReader(memstream).ReadInt32();
					//MyDebug.WriteLine("Expecting msg of {0} bytes", m_expectedLen);

					if (m_expectedLen > m_buffer.Length)
						throw new Exception();
				}

				if (m_bufferUsed >= m_expectedLen)
				{
					var memstream = new MemoryStream(m_buffer, 4, m_expectedLen - 4);
					var msg = m_serializer.Deserialize(memstream);
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
					//MyDebug.WriteLine("{0} != {1}", m_expectedLen, m_bufferUsed);
					break;
				}
			}

			BeginRead();
		}

		protected abstract void ReceiveMessage(Message msg);

		public void Disconnect()
		{
			m_client.Client.Shutdown(SocketShutdown.Both);
			m_client.Client.Close();
			m_client.Close();
		}

		public virtual void Send(Message msg)
		{
			var bytes = m_serializer.Send(m_client.GetStream(), msg);
			this.SentMessages++;
			this.SentBytes += bytes;
		}
	}
}
