using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using Dwarrowdelf.Messages;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public sealed class PipeConnection : IConnection
	{
		PipeStream m_stream;

		Thread m_deserializerThread;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		public event Action NewMessageEvent;

		ConcurrentQueue<Message> m_msgQueue = new ConcurrentQueue<Message>();

		volatile bool m_isConnected;

		public PipeConnection(PipeStream stream)
		{
			trace.Header = "pipe";

			trace.TraceInformation("New Connection");

			m_isConnected = true;

			m_stream = stream;

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
				DH.Dispose(ref m_stream);
			}

			m_disposed = true;
		}
		#endregion

		public bool IsConnected { get { return m_isConnected; } }

		public bool TryGetMessage(out Message msg)
		{
			return m_msgQueue.TryDequeue(out msg);
		}

		public async Task<Message> GetMessageAsync()
		{
			Message msg;

			if (TryGetMessage(out msg))
				return msg;

			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

			Action handler = () =>
			{
				tcs.TrySetResult(true);
			};

			this.NewMessageEvent += handler;

			if (TryGetMessage(out msg) == false)
			{
				await tcs.Task;

				if (TryGetMessage(out msg) == false)
				{
					Thread.MemoryBarrier();

					if (this.IsConnected == false)
						return null;

					throw new Exception();
				}
			}

			this.NewMessageEvent -= handler;

			return msg;
		}

		void DeserializerMain()
		{
			try
			{
				while (true)
				{
					var msg = Serializer.Deserialize(m_stream);

					m_msgQueue.Enqueue(msg);

					var ev = this.NewMessageEvent;
					if (ev != null)
						ev();
				}
			}
			catch (Exception e)
			{
				m_isConnected = false;

				m_stream.Close();

				trace.TraceInformation("[RX]: error {0}", e.Message);

				var ev = this.NewMessageEvent;
				if (ev != null)
					ev();
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

			m_isConnected = false;

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
