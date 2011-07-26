using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Dwarrowdelf.Messages;

namespace Dwarrowdelf.Server
{
	public class ServerConnection
	{
		IConnection m_connection;
		bool m_userLoggedIn;
		Player m_user;

		System.Collections.Concurrent.ConcurrentQueue<Message> m_msgQueue = new System.Collections.Concurrent.ConcurrentQueue<Message>();

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		GameEngine m_engine;

		public ServerConnection(GameEngine engine, IConnection connection)
		{
			m_engine = engine;
			m_connection = connection;

			trace.Header = "ServerConnection";
			trace.TraceInformation("New ServerConnection");

			m_connection.ReceiveEvent += OnReceiveMessage;
			m_connection.DisconnectEvent += OnDisconnect;
		}

		public void Start()
		{
			m_connection.BeginRead();
		}

		void Cleanup()
		{
			m_connection.ReceiveEvent -= OnReceiveMessage;
			m_connection.DisconnectEvent -= OnDisconnect;
			m_connection = null;
		}

		public void Disconnect()
		{
			trace.TraceInformation("Disconnect");

			m_connection.Disconnect();
		}

		void OnDisconnect()
		{
			trace.TraceInformation("OnDisconnect");

			m_engine.SignalWorld();
		}

		void OnReceiveMessage(Message m)
		{
			trace.TraceVerbose("OnReceiveMessage");

			m_msgQueue.Enqueue(m);
			m_engine.SignalWorld();
		}

		public bool IsConnected
		{
			get { return m_connection != null && m_connection.IsConnected; }
		}

		public void HandleNewMessages()
		{
			trace.TraceVerbose("HandleNewMessages, count = {0}", m_msgQueue.Count);

			Message msg;
			while (m_msgQueue.TryDequeue(out msg))
			{
				if (msg is LogOnRequestMessage)
					HandleLoginMessage((LogOnRequestMessage)msg);
				else if (msg is LogOutRequestMessage)
					HandleLogOutMessage((LogOutRequestMessage)msg);
				else
				{
					if (m_user != null)
						m_user.OnReceiveMessage(msg);
					else
						trace.TraceWarning("HandleNewMessages: m_user == null");
				}
			}

			if (!m_connection.IsConnected)
			{
				trace.TraceInformation("HandleNewMessages, disconnected");

				if (m_userLoggedIn)
				{
					m_user.Connection = null;
					m_user = null;
					m_userLoggedIn = false;
				}

				Cleanup();
			}
		}

		void HandleLoginMessage(LogOnRequestMessage msg)
		{
			trace.TraceInformation("HandleLoginMessage");

			string name = msg.Name;

			trace.Header = String.Format("ServerConnection({0})", name);
			trace.TraceInformation("LogOnRequestMessage");

			m_userLoggedIn = true;

			m_user = GetPlayer(name);
			m_user.Connection = this;

			Send(new Messages.LogOnReplyBeginMessage() { IsSeeAll = m_user.IsSeeAll, Tick = m_engine.World.TickNumber });

			if (m_user.IsSeeAll)
			{
				foreach (var env in m_engine.World.Environments)
					env.SendTo(m_user);
			}

			Send(new Messages.LogOnReplyEndMessage());
		}

		Player GetPlayer(string name)
		{
			int userID; // from universal user object

			if (name == "tomba")
				userID = 1;
			else
				throw new Exception();

			var player = m_engine.FindPlayer(userID);

			if (player == null)
				player = m_engine.CreatePlayer(userID);

			return player;
		}

		void HandleLogOutMessage(LogOutRequestMessage msg)
		{
			trace.TraceInformation("HandleLogOutMessage");

			Send(new Messages.LogOutReplyMessage());

			m_user.Connection = null;
			m_user = null;
			m_userLoggedIn = false;

			Disconnect();
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
		}

		public void Send(IEnumerable<ClientMessage> msgs)
		{
			foreach (var msg in msgs)
				Send(msg);
		}
	}
}
