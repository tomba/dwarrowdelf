using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dwarrowdelf.Messages;

namespace Dwarrowdelf.Client
{
	public class ClientNetStatistics : INotifyPropertyChanged, INetStatCollector
	{
		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		Dictionary<Type, int> m_sentCountMap = new Dictionary<Type, int>();

		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }
		Dictionary<Type, int> m_receivedCountMap = new Dictionary<Type, int>();

		public sealed class MessageCountStore : INotifyPropertyChanged
		{
			int m_count;

			public Type Type { get; set; }
			public int Count { get { return m_count; } set { m_count = value; Notify("Count"); } }

			void Notify(string property)
			{
				if (this.PropertyChanged != null)
					this.PropertyChanged(this, new PropertyChangedEventArgs(property));
			}

			#region INotifyPropertyChanged Members

			public event PropertyChangedEventHandler PropertyChanged;

			#endregion
		}

		ObservableCollection<MessageCountStore> m_receivedMessageCounts = new ObservableCollection<MessageCountStore>();
		public ObservableCollection<MessageCountStore> ReceivedMessageCounts { get { return m_receivedMessageCounts; } }

		ObservableCollection<MessageCountStore> m_sentMessageCounts = new ObservableCollection<MessageCountStore>();
		public ObservableCollection<MessageCountStore> SentMessageCounts { get { return m_sentMessageCounts; } }

		SynchronizationContext m_syncCtx;
		int m_messageReportingEnableCount;

		public ClientNetStatistics()
		{
			m_syncCtx = SynchronizationContext.Current;
		}

		public void EnableMessageReporting()
		{
			int v = Interlocked.Increment(ref m_messageReportingEnableCount);

			if (v == 1)
				Refresh();
		}

		public void DisableMessageReporting()
		{
			Interlocked.Decrement(ref m_messageReportingEnableCount);
		}

		public void Reset()
		{
			lock (m_sentCountMap)
			{
				this.SentMessages = 0;
				this.SentBytes = 0;
				m_sentCountMap.Clear();
			}

			lock (m_receivedCountMap)
			{
				this.ReceivedMessages = 0;
				this.ReceivedBytes = 0;
				m_receivedCountMap.Clear();
			}

			m_receivedMessageCounts.Clear();
			m_sentMessageCounts.Clear();

			Notify("SentMessages");
			Notify("SentBytes");
			Notify("ReceivedMessages");
			Notify("ReceivedBytes");
		}

		void DoRefresh()
		{
			Notify("ReceivedMessages");
			Notify("ReceivedBytes");

			Notify("SentMessages");
			Notify("SentBytes");

			if (m_messageReportingEnableCount > 0)
			{
				KeyValuePair<Type, int>[] sent;
				lock (m_sentCountMap)
					sent = m_sentCountMap.ToArray();

				KeyValuePair<Type, int>[] received;
				lock (m_receivedCountMap)
					received = m_receivedCountMap.ToArray();

				foreach (var kvp in received)
				{
					var t = kvp.Key;
					var c = kvp.Value;

					var entry = m_receivedMessageCounts.SingleOrDefault(i => i.Type == t);
					if (entry == null)
					{
						entry = new MessageCountStore() { Type = t, Count = c };
						m_receivedMessageCounts.Add(entry);
					}
					else
					{
						entry.Count = c;
					}
				}

				foreach (var kvp in sent)
				{
					var t = kvp.Key;
					var c = kvp.Value;

					var entry = m_sentMessageCounts.SingleOrDefault(i => i.Type == t);
					if (entry == null)
					{
						entry = new MessageCountStore() { Type = t, Count = c };
						m_sentMessageCounts.Add(entry);
					}
					else
					{
						entry.Count = c;
					}
				}
			}
		}

		bool m_refreshQueued;

		// not quite correct, but good enough for this...
		void Refresh()
		{
			if (m_refreshQueued)
				return;

			m_refreshQueued = true;
			m_syncCtx.Post(async o => { await Task.Delay(250); m_refreshQueued = false; DoRefresh(); }, null);
		}

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		void INetStatCollector.OnMessageReceived(Type msgType, int bytes)
		{
			lock (m_receivedCountMap)
			{
				this.ReceivedMessages++;
				this.ReceivedBytes += bytes;

				int c = 0;

				m_receivedCountMap.TryGetValue(msgType, out c);

				c++;
				m_receivedCountMap[msgType] = c;
			}

			Refresh();
		}

		void INetStatCollector.OnMessageSent(Type msgType, int bytes)
		{
			lock (m_sentCountMap)
			{
				this.SentMessages++;
				this.SentBytes += bytes;

				int c = 0;

				m_sentCountMap.TryGetValue(msgType, out c);

				c++;
				m_sentCountMap[msgType] = c;
			}

			Refresh();
		}
	}
}
