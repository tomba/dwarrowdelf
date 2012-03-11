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

		Message Receive();

		void Start(Action<Message> receiveCallback, Action disconnectCallback);
		void Send(Message msg);
		void Disconnect();
	}

	public sealed class Connection : IConnection
	{
		Socket m_socket;
		GameNetStream m_netStream;

		public bool IsConnected { get { return m_socket.Connected; } }

		Action<Message> m_receiveCallback;
		Action m_disconnectCallback;

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		object m_sendLock = new object();

		public const int PORT = 9999;

		const uint MAGIC = 0x12345678;

		const int RECV_BUF = 65536;
		const int SEND_BUF = 65536;

		Thread m_deserializerThread;

		public Connection(Socket socket)
		{
			trace.Header = socket.RemoteEndPoint.ToString();

			trace.TraceInformation("New Connection");

			if (socket.Connected == false)
				throw new Exception();

			m_socket = socket;
			m_netStream = new GameNetStream(socket, RECV_BUF, SEND_BUF);
		}

		public void Start(Action<Message> receiveCallback, Action disconnectCallback)
		{
			Debug.Assert(m_socket != null);
			Debug.Assert(m_deserializerThread == null);

			m_receiveCallback = receiveCallback;
			m_disconnectCallback = disconnectCallback;

			m_deserializerThread = new Thread(DeserializerMain);
			m_deserializerThread.Start();
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
			catch (Exception e)
			{
				trace.TraceInformation("[RX]: socket error {0}", e.Message);

				m_socket.Shutdown(SocketShutdown.Both);
				m_socket.Close();

				m_disconnectCallback();
			}

			trace.TraceVerbose("Deserializer thread ending");
		}

		public Message Receive()
		{
			var recvStream = m_netStream;

			int len = recvStream.ReadBytes;

			uint magic =
				(uint)recvStream.ReadByte() |
				(uint)recvStream.ReadByte() << 8 |
				(uint)recvStream.ReadByte() << 16 |
				(uint)recvStream.ReadByte() << 24;

			if (magic != MAGIC)
				throw new Exception();

			trace.TraceVerbose("[RX] Deserializing");

			var msg = Serializer.Deserialize(recvStream);

			len = recvStream.ReadBytes - len;

			trace.TraceVerbose("[RX] Deserialized {0} bytes, {1}", len, msg.GetType().Name);

			this.ReceivedMessages++;
			this.ReceivedBytes += len;

			return msg;
		}

		public void Disconnect()
		{
			trace.TraceInformation("Disconnect");

			m_socket.Shutdown(SocketShutdown.Both);
			m_socket.Disconnect(false);

			m_deserializerThread.Join();

			trace.TraceInformation("Disconnect done");
		}

		public void Send(Message msg)
		{
			if (m_socket.Connected == false)
			{
				trace.TraceVerbose("[TX] socket not connected, skipping send");
				return;
			}

			try
			{
				lock (m_sendLock)
				{
					trace.TraceVerbose("[TX] sending {0}", msg.GetType().Name);

					var stream = m_netStream;

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
			}
			catch (SocketException e)
			{
				var error = e.SocketErrorCode;

				trace.TraceError("[TX]: socket error {0}", error);
			}
		}
	}
}
