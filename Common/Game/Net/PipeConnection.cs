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

		#region IDisposable

		bool m_disposed;

		~PipeConnection()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (m_disposed)
				return;

			if (disposing)
			{
				// Managed cleanup code here, while managed refs still valid
				DH.Dispose(ref m_pipeStream);
				DH.Dispose(ref m_stream);
				DH.Dispose(ref m_msgQueue);
			}

			m_disposed = true;
		}
		#endregion

		public bool IsConnected { get { return m_pipeStream.IsConnected; } }

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

					m_msgQueue.Add(msg);

					var ev = this.NewMessageEvent;
					if (ev != null)
						ev();
				}
			}
			catch (Exception e)
			{
				trace.TraceInformation("[RX]: error {0}", e.Message);

				var ev = this.NewMessageEvent;
				if (ev != null)
					ev();

				m_msgQueue.CompleteAdding();
			}

			trace.TraceVerbose("Deserializer thread ending");
		}


		public void Send(Message msg)
		{
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
