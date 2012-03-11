
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Dwarrowdelf.Messages;
using System.Runtime.Serialization;
using System.IO;
using System.ComponentModel;

using Dwarrowdelf;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Client
{
	sealed class ClientConnection
	{
		public ClientNetStatistics Stats { get; private set; }

		IConnection m_connection;
		ClientUser m_user;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		public event Action DisconnectEvent;

		World m_world;

		public ClientConnection()
		{
			trace.Header = "ClientConnection";

			this.Stats = new ClientNetStatistics();
		}

		public void BeginLogOn(string name, Action<ClientUser, string> callback)
		{
			trace.Header = String.Format("ClientConnection({0})", name);

			try
			{
				var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				var localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
				socket.Bind(localEndPoint);

				var port = Connection.PORT;

				var remoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);

				trace.Header = socket.LocalEndPoint.ToString();
				trace.TraceInformation("BeginConnect to {0}", remoteEndPoint);

				socket.Connect(remoteEndPoint);

				m_connection = new Connection(socket);

				m_connection.Send(new Messages.LogOnRequestMessage() { Name = name });

				var msg = m_connection.Receive();

				var reply = msg as Messages.LogOnReplyBeginMessage;

				if (reply == null)
					throw new Exception();

				m_world = new World();
				GameData.Data.World = m_world;

				m_world.SetLivingVisionMode(reply.LivingVisionMode);
				m_world.SetTick(reply.Tick);

				m_user = new ClientUser(this, m_world, reply.IsSeeAll);

				callback(m_user, null);

				m_connection.Start(_OnReceiveMessage, _OnDisconnected);
			}
			catch (Exception e)
			{
				callback(null, e.Message);
			}
		}

		public void SendLogOut()
		{
			m_connection.Send(new Messages.LogOutRequestMessage());
		}

		void _OnDisconnected()
		{
			System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(OnDisconnected));
		}

		void OnDisconnected()
		{
			trace.TraceInformation("OnDisconnect");

			if (DisconnectEvent != null)
				DisconnectEvent();
		}

		void _OnReceiveMessage(Message msg)
		{
			System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action<ClientMessage>(OnReceiveMessage), msg);
		}

		void OnReceiveMessage(ClientMessage msg)
		{
			this.Stats.ReceivedBytes = m_connection.ReceivedBytes;
			this.Stats.ReceivedMessages = m_connection.ReceivedMessages;
			this.Stats.AddReceivedMessages(msg);

			m_user.OnReceiveMessage(msg);
		}

		public void Send(ServerMessage msg)
		{
			if (m_connection == null)
			{
				trace.TraceWarning("Send: m_connection == null");
				return;
			}

			if (!m_connection.IsConnected)
			{
				trace.TraceWarning("Send: m_connection.IsConnected == false");
				return;
			}

			m_connection.Send(msg);

			this.Stats.SentBytes = m_connection.SentBytes;
			this.Stats.SentMessages = m_connection.SentMessages;
			this.Stats.AddSentMessages(msg);
		}
	}

	class ClientNetStatistics : INotifyPropertyChanged
	{
		int m_sentMessages;
		int m_sentBytes;
		int m_receivedMessages;
		int m_receivedBytes;

		public int SentMessages { get { return m_sentMessages; } set { m_sentMessages = value; Refresh(); } }
		public int SentBytes { get { return m_sentBytes; } set { m_sentBytes = value; Refresh(); } }
		public int ReceivedMessages { get { return m_receivedMessages; } set { m_receivedMessages = value; Refresh(); } }
		public int ReceivedBytes { get { return m_receivedBytes; } set { m_receivedBytes = value; Refresh(); } }

		DispatcherTimer m_timer;

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

		public ClientNetStatistics()
		{
			m_timer = new DispatcherTimer();
			m_timer.Interval = TimeSpan.FromMilliseconds(250);
			m_timer.Tick += RefreshTick;
		}

		void RefreshTick(object sender, EventArgs e)
		{
			m_timer.Stop();

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

		void Refresh()
		{
			if (m_timer.IsEnabled)
				return;

			m_timer.Start();
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
