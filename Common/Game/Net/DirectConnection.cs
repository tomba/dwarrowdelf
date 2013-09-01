using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public sealed class DirectConnection : MarshalByRefObject, IConnection
	{
		DirectConnection m_remoteConnection;

		ConcurrentQueue<Message> m_msgQueue = new ConcurrentQueue<Message>();

		public event Action NewMessageEvent;

		public DirectConnection()
		{

		}

		public DirectConnection(DirectConnection remote)
		{
			m_remoteConnection = remote;
			m_remoteConnection.SetRemote(this);
		}

		#region IDisposable

		bool m_disposed;

		~DirectConnection()
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
			}

			m_disposed = true;
		}
		#endregion

		void SetRemote(DirectConnection remote)
		{
			m_remoteConnection = remote;
		}

		public bool IsConnected
		{
			get { return m_remoteConnection != null; }
		}

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
					if (this.IsConnected == false)
						return null;

					throw new Exception();
				}
			}

			this.NewMessageEvent -= handler;

			return msg;
		}

		void Enqueue(Message msg)
		{
			m_msgQueue.Enqueue(msg);

			var ev = this.NewMessageEvent;
			if (ev != null)
				ev();
		}

		public void Send(Message msg)
		{
			m_remoteConnection.Enqueue(msg);
		}

		void RemoteDisconnect()
		{
			m_remoteConnection = null;

			var ev = this.NewMessageEvent;
			if (ev != null)
				ev();
		}

		public void Disconnect()
		{
			var remote = m_remoteConnection;

			m_remoteConnection = null;

			remote.RemoteDisconnect();

			var ev = this.NewMessageEvent;
			if (ev != null)
				ev();
		}

		public static DirectConnection Connect(IGame game)
		{
			var connection = new DirectConnection();

			game.Connect(connection);

			if (connection.m_remoteConnection == null)
				throw new Exception();

			return connection;
		}
	}
}
