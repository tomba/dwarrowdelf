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
	public class ClientNetStatistics : INotifyPropertyChanged
	{
		int m_sentMessages;
		int m_sentBytes;
		int m_receivedMessages;
		int m_receivedBytes;

		public int SentMessages { get { return m_sentMessages; } set { m_sentMessages = value; Refresh(); } }
		public int SentBytes { get { return m_sentBytes; } set { m_sentBytes = value; Refresh(); } }
		public int ReceivedMessages { get { return m_receivedMessages; } set { m_receivedMessages = value; Refresh(); } }
		public int ReceivedBytes { get { return m_receivedBytes; } set { m_receivedBytes = value; Refresh(); } }

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

		Dictionary<Type, int> m_receivedCountMap = new Dictionary<Type, int>();
		ObservableCollection<MessageCountStore> m_receivedMessageCounts = new ObservableCollection<MessageCountStore>();
		public ObservableCollection<MessageCountStore> ReceivedMessageCounts { get { return m_receivedMessageCounts; } }

		Dictionary<Type, int> m_sentCountMap = new Dictionary<Type, int>();
		ObservableCollection<MessageCountStore> m_sentMessageCounts = new ObservableCollection<MessageCountStore>();
		public ObservableCollection<MessageCountStore> SentMessageCounts { get { return m_sentMessageCounts; } }

		SynchronizationContext m_syncCtx;

		public ClientNetStatistics()
		{
			m_syncCtx = SynchronizationContext.Current;
		}

		public void Reset()
		{
			m_sentMessages = 0;
			m_sentBytes = 0;
			m_receivedMessages = 0;
			m_receivedBytes = 0;

			Notify("SentMessages");
			Notify("SentBytes");
			Notify("ReceivedMessages");
			Notify("ReceivedBytes");

			m_receivedCountMap.Clear();
			m_receivedMessageCounts.Clear();
			m_sentCountMap.Clear();
			m_sentMessageCounts.Clear();
		}

		void DoRefresh()
		{
			Notify("SentMessages");
			Notify("SentBytes");
			Notify("ReceivedMessages");
			Notify("ReceivedBytes");

			foreach (var kvp in m_receivedCountMap)
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

			foreach (var kvp in m_sentCountMap)
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

		bool m_refreshQueued;

		// not quite correct, but good enough for this...
		void Refresh()
		{
			if (m_refreshQueued)
				return;

			m_refreshQueued = true;
			m_syncCtx.Post(async o => { await Task.Delay(250); m_refreshQueued = false; DoRefresh(); }, null);
		}

		public void AddReceivedMessages(ClientMessage msg)
		{
			int c = 0;

			m_receivedCountMap.TryGetValue(msg.GetType(), out c);

			c++;
			m_receivedCountMap[msg.GetType()] = c;

			Refresh();
		}

		public void AddSentMessages(ServerMessage msg)
		{
			int c = 0;

			m_sentCountMap.TryGetValue(msg.GetType(), out c);

			c++;
			m_sentCountMap[msg.GetType()] = c;

			Refresh();
		}

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
