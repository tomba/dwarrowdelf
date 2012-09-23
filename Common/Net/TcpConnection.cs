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
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public sealed class TcpConnection : IConnection
	{
		Socket m_socket;
		GameNetStream m_netStream;

		public bool IsConnected { get { return m_socket.Connected; } }

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		object m_sendLock = new object();

		public const int PORT = 9999;

		const uint MAGIC = 0x12345678;

		Thread m_deserializerThread;

		ConcurrentQueue<Message> m_msgQueue = new ConcurrentQueue<Message>();

		public event Action NewMessageEvent;

		public TcpConnection(Socket socket)
		{
			trace.Header = socket.RemoteEndPoint.ToString();

			trace.TraceInformation("New Connection");

			if (socket.Connected == false)
				throw new Exception();

			m_socket = socket;
			m_netStream = new GameNetStream(socket);

			m_deserializerThread = new Thread(DeserializerMain);
			m_deserializerThread.Start();
		}

		public bool TryGetMessage(out Message msg)
		{
			return m_msgQueue.TryDequeue(out msg);
		}

		void DeserializerMain()
		{
			try
			{
				while (true)
				{
					var msg = ReceiveInternal();

					m_msgQueue.Enqueue(msg);

					var ev = this.NewMessageEvent;
					if (ev != null)
						ev();
				}
			}
			catch (Exception e)
			{
				trace.TraceInformation("[RX]: socket error {0}", e.Message);

				m_socket.Shutdown(SocketShutdown.Both);
				m_socket.Close();

				var ev = this.NewMessageEvent;
				if (ev != null)
					ev();
			}

			trace.TraceVerbose("Deserializer thread ending");
		}

		Message ReceiveInternal()
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

			try
			{
				// this will cause the deserializer thread to wake up
				m_socket.Shutdown(SocketShutdown.Both);
			}
			catch (SocketException e)
			{
				trace.TraceError("Error when disconnecting: {0}", e.Message);
			}

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

		public async static Task<TcpConnection> ConnectAsync()
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			var localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
			socket.Bind(localEndPoint);

			var port = TcpConnection.PORT;

			var remoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);

			await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, remoteEndPoint, null);

			return new TcpConnection(socket);
		}
	}
}
