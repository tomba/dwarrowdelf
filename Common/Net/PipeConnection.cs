using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using Dwarrowdelf.Messages;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;

namespace Dwarrowdelf
{
	public class PipeConnection : IConnection
	{
		PipeStream m_pipeStream;
		BufferedStream m_stream;

		Thread m_deserializerThread;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		public event Action NewMessageEvent;
		public event Action DisconnectEvent;

		BlockingCollection<Message> m_msgQueue = new BlockingCollection<Message>();

		public PipeConnection(PipeStream stream)
		{
			trace.Header = "pipe";

			trace.TraceInformation("New Connection");

			m_pipeStream = stream;
			m_stream = new BufferedStream(m_pipeStream);

			m_deserializerThread = new Thread(DeserializerMain);
			m_deserializerThread.Start();
		}

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		public bool IsConnected { get { return m_pipeStream.IsConnected; } }

		public Message GetMessage()
		{
			return m_msgQueue.Take();
		}

		public bool TryGetMessage(out Message msg)
		{
			return m_msgQueue.TryTake(out msg);
		}

		void DeserializerMain()
		{
			try
			{
				while (true)
				{
					var msg = Serializer.Deserialize(m_stream);
					this.ReceivedMessages++;

					m_msgQueue.Add(msg);

					var ev = this.NewMessageEvent;
					if (ev != null)
						ev();
				}
			}
			catch (Exception e)
			{
				trace.TraceInformation("[RX]: error {0}", e.Message);

				if (this.DisconnectEvent != null)
					DisconnectEvent();

				// XXX this is to wake up possible waiters in GetMessage()
				var ev = this.NewMessageEvent;
				if (ev != null)
					ev();
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
