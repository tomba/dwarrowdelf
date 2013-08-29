using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	sealed class GameDispatcher
	{
		public Thread Thread { get; private set; }

		ConcurrentQueue<KeyValuePair<SendOrPostCallback, object>> m_queue = new ConcurrentQueue<KeyValuePair<SendOrPostCallback, object>>();

		volatile bool m_exit = false;
		AutoResetEvent m_signal = new AutoResetEvent(false);

		public GameDispatcher()
		{
			this.Thread = Thread.CurrentThread;
		}

		public void Run(Func<bool> workFunc)
		{
			SynchronizationContext oldSyncCtx = null;

			try
			{
				oldSyncCtx = SynchronizationContext.Current;

				var syncCtx = new GameSyncContext(this);
				SynchronizationContext.SetSynchronizationContext(syncCtx);

				bool again = true;

				while (m_exit == false)
				{
					if (!again)
						m_signal.WaitOne();

					KeyValuePair<SendOrPostCallback, object> entry;
					if (m_queue.TryDequeue(out entry))
					{
						entry.Key(entry.Value);
						again |= m_queue.Count > 0;
					}

					again |= workFunc();
				}
			}
			finally
			{
				SynchronizationContext.SetSynchronizationContext(oldSyncCtx);
			}
		}

		public void Shutdown()
		{
			m_exit = true;
			Thread.MemoryBarrier(); // XXX this shouldn't be needed?
			Signal();
		}

		public void Signal()
		{
			m_signal.Set();
		}

		public void BeginInvoke(SendOrPostCallback d, object state)
		{
			m_queue.Enqueue(new KeyValuePair<SendOrPostCallback, object>(d, state));
			m_signal.Set();
		}

		public bool CheckAccess()
		{
			return this.Thread == Thread.CurrentThread;
		}

		public void VerifyAccess()
		{
			if (!this.CheckAccess())
				throw new InvalidOperationException("VerifyAccess");
		}

		sealed class GameSyncContext : SynchronizationContext
		{
			GameDispatcher m_dispatcher;

			public GameSyncContext(GameDispatcher engine)
			{
				m_dispatcher = engine;
			}

			public override SynchronizationContext CreateCopy()
			{
				return new GameSyncContext(m_dispatcher);
			}

			public override void Post(SendOrPostCallback d, object state)
			{
				m_dispatcher.BeginInvoke(d, state);
			}

			public override void Send(SendOrPostCallback d, object state)
			{
				throw new NotImplementedException();
			}
		}

	}
}
