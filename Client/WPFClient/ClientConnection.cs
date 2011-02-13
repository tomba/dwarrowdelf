
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

namespace Dwarrowdelf.Client
{
	class ClientNetStatistics : INotifyPropertyChanged
	{
		public int SentMessages { get; set; }
		public int SentBytes { get; set; }
		public int ReceivedMessages { get; set; }
		public int ReceivedBytes { get; set; }

		public void Refresh()
		{
			Notify("SentMessages");
			Notify("SentBytes");
			Notify("ReceivedMessages");
			Notify("ReceivedBytes");
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

	class ClientConnection
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
				m_connection.ConnectEvent -= OnConnect;
				m_connection.ReceiveEvent -= _OnReceiveMessage;
				m_connection.DisconnectEvent -= _OnDisconnected;
				m_connection = null;
			}
		}

		public void BeginLogOn(string name, Action<ClientUser, string> callback)
		{
			m_connection = new Connection();
			m_connection.ConnectEvent += OnConnect;
			m_connection.ReceiveEvent += _OnReceiveMessage;
			m_connection.DisconnectEvent += _OnDisconnected;

			m_logOnCallback = callback;
			m_logOnName = name;
			m_connection.BeginConnect();
		}

		void OnConnect(string error)
		{
			if (error != null)
			{
				m_logOnCallback(null, error);
				Cleanup();
			}
			else
			{
				m_connection.BeginRead();
				Send(new Messages.LogOnRequestMessage() { Name = m_logOnName });
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
			System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action<ServerMessage>(OnReceiveMessage), msg);
		}

		void OnReceiveMessage(ServerMessage msg)
		{
			this.Stats.ReceivedBytes = m_connection.ReceivedBytes;
			this.Stats.ReceivedMessages = m_connection.ReceivedMessages;
			this.Stats.Refresh();

			if (msg is LogOnReplyMessage)
				HandleLoginReplyMessage((LogOnReplyMessage)msg);
			else if (msg is LogOutReplyMessage)
				HandleLogOutReplyMessage((LogOutReplyMessage)msg);
			else
				m_user.OnReceiveMessage(msg);
		}

		void HandleLoginReplyMessage(LogOnReplyMessage msg)
		{
			trace.Header = String.Format("ClientConnection({0})", msg.UserID);
			trace.TraceInformation("HandleLoginReplyMessage");

			m_user = new ClientUser(this, m_world, msg.UserID, msg.IsSeeAll);

			m_logOnCallback(m_user, null);

			m_logOnCallback = null;
			m_logOnName = null;
		}

		void HandleLogOutReplyMessage(ServerMessage msg)
		{
			trace.TraceInformation("HandleLogOutReplyMessage");
			if (this.LogOutEvent != null)
				this.LogOutEvent();
		}

		public void Send(ClientMessage msg)
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
			this.Stats.Refresh();
		}
	}
}
