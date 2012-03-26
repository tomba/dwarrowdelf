using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using Dwarrowdelf.Messages;
using System.Threading;
using System.IO;

namespace Dwarrowdelf
{
	public class PipeConnection : IConnection
	{
		PipeStream m_pipeStream;
		BufferedStream m_stream;

		Action<Message> m_receiveCallback;
		Action m_disconnectCallback;

		Thread m_deserializerThread;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		public PipeConnection(PipeStream stream)
		{
			trace.Header = "pipe";

			trace.TraceInformation("New Connection");

			m_pipeStream = stream;
			m_stream = new BufferedStream(m_pipeStream);
		}

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		public bool IsConnected { get { return m_pipeStream.IsConnected; } }

		public void Start(Action<Message> receiveCallback, Action disconnectCallback)
		{
			m_receiveCallback = receiveCallback;
			m_disconnectCallback = disconnectCallback;

			m_deserializerThread = new Thread(DeserializerMain);
			m_deserializerThread.Start();
		}

		public Message Receive()
		{
			var msg = Serializer.Deserialize(m_stream);

			return msg;
		}

		void DeserializerMain()
		{
			try
			{
				while (true)
				{
					var msg = Receive();
					this.ReceivedMessages++;

					m_receiveCallback(msg);
				}
			}
			catch (Exception e)
			{
				trace.TraceInformation("[RX]: error {0}", e.Message);

				m_disconnectCallback();
			}

			trace.TraceVerbose("Deserializer thread ending");
		}


		public void Send(Message msg)
		{
			this.SentMessages++;

			Serializer.Serialize(m_stream, msg);

			m_stream.Flush();
		}

		public void Disconnect()
		{
			trace.TraceInformation("Disconnect");

			m_stream.Close();
			m_deserializerThread.Join();

			trace.TraceInformation("Disconnect done");
		}

		public static PipeConnection Connect()
		{
			var stream = new NamedPipeClientStream(".", "Dwarrowdelf.Pipe", PipeDirection.InOut, PipeOptions.Asynchronous);

			stream.Connect();

			return new PipeConnection(stream);
		}
	}
}
