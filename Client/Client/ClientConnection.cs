
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

	sealed class ClientConnection
	{
		public ClientNetStatistics Stats { get; private set; }

		IConnection m_connection;
		ClientUser m_user;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		public event Action DisconnectEvent;
		public event Action LogOutEvent;

		World m_world;

		string m_logOnName;
		Action<ClientUser, string> m_logOnCallback;

		public ClientConnection(World world)
		{
			m_world = world;

			trace.Header = "ClientConnection";

			this.Stats = new ClientNetStatistics();
		}

		void Cleanup()
		{
			m_logOnCallback = null;
			m_logOnName = null;

			if (m_connection != null)
			{
				m_connection.ReceiveEvent -= _OnReceiveMessage;
				m_connection.DisconnectEvent -= _OnDisconnected;
				m_connection = null;
			}
		}

		public void BeginLogOn(string name, Action<ClientUser, string> callback)
		{
			trace.Header = String.Format("ClientConnection({0})", name);

			m_logOnCallback = callback;
			m_logOnName = name;

			try
			{
				var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				var localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
				socket.Bind(localEndPoint);

				var port = Connection.PORT;

				var remoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);

				trace.Header = socket.LocalEndPoint.ToString();
				trace.TraceInformation("BeginConnect to {0}", remoteEndPoint);

				socket.BeginConnect(remoteEndPoint, ConnectCallback, socket);
			}
			catch (Exception e)
			{
				Cleanup();
				m_logOnCallback(null, e.Message);
			}
		}

		void ConnectCallback(IAsyncResult ar)
		{
			trace.TraceInformation("ConnectCallback");

			var socket = (Socket)ar.AsyncState;

			try
			{
				socket.EndConnect(ar);

				m_connection = new Connection(socket);
				m_connection.ReceiveEvent += _OnReceiveMessage;
				m_connection.DisconnectEvent += _OnDisconnected;

				m_connection.BeginRead();
				Send(new Messages.LogOnRequestMessage() { Name = m_logOnName });
			}
			catch (Exception e)
			{
				Cleanup();
				m_logOnCallback(null, e.Message);
			}
		}

		public void SendLogOut()
		{
			if (m_user != null)
			{
				m_connection.Send(new Messages.LogOutRequestMessage());
			}
			else
			{
				m_connection.Disconnect();
				if (this.LogOutEvent != null)
					this.LogOutEvent();
				Cleanup();
			}
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

			Cleanup();
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

			if (msg is LogOnReplyBeginMessage)
				HandleLoginReplyBeginMessage((LogOnReplyBeginMessage)msg);
			else if (msg is LogOnReplyEndMessage)
				HandleLoginReplyEndMessage((LogOnReplyEndMessage)msg);
			else if (msg is LogOutReplyMessage)
				HandleLogOutReplyMessage((LogOutReplyMessage)msg);
			else
				m_user.OnReceiveMessage(msg);
		}

		DateTime m_logOnStartTime;

		void HandleLoginReplyBeginMessage(LogOnReplyBeginMessage msg)
		{
			trace.TraceInformation("LogOnReplyBeginMessage");

			m_logOnStartTime = DateTime.Now;

			m_user = new ClientUser(this, m_world, msg.IsSeeAll);
			GameData.Data.World.SetLivingVisionMode(msg.LivingVisionMode);
			GameData.Data.World.SetTick(msg.Tick);

			m_logOnCallback(m_user, null);
			m_logOnCallback = null;
			m_logOnName = null;
		}

		void HandleLoginReplyEndMessage(LogOnReplyEndMessage msg)
		{
			trace.TraceInformation("LogOnReplyEndMessage");

			var time = DateTime.Now - m_logOnStartTime;
			Trace.TraceInformation("LogOn took {0}", time);

			// XXX we don't currently do anything here. We could keep the login dialog open until this, but we need to call
			// logonCallback in HandleLoginReplyBeginMessage, so that GameData.User etc are set
		}

		void HandleLogOutReplyMessage(ClientMessage msg)
		{
			trace.TraceInformation("HandleLogOutReplyMessage");

			if (this.LogOutEvent != null)
				this.LogOutEvent();
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
}
