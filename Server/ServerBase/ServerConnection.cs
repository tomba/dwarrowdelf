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
		World m_world;
		bool m_userLoggedIn;
		static int s_userIDs = 1;
		ServerUser m_user;

		System.Collections.Concurrent.ConcurrentQueue<Message> m_msgQueue = new System.Collections.Concurrent.ConcurrentQueue<Message>();

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		public ServerConnection(IConnection connection)
		{
			m_connection = connection;

			trace.Header = "ServerConnection";
			trace.TraceInformation("New ServerConnection");

			m_connection.ReceiveEvent += OnReceiveMessage;
			m_connection.DisconnectEvent += OnDisconnect;
			m_connection.BeginRead();
		}

		public void Init(World world)
		{
			m_world = world;
			m_world.AddConnection(this);
		}

		void Cleanup()
		{
			m_world.RemoveConnection(this);
			m_world = null;

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

			m_world.SignalWorld();
		}

		void OnReceiveMessage(Message m)
		{
			trace.TraceVerbose("OnReceiveMessage");

			m_msgQueue.Enqueue(m);
			m_world.SignalWorld();
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
					m_user.UnInit();
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

			var userID = s_userIDs++;
			m_userLoggedIn = true;

			m_user = new ServerUser(userID);

			m_connection.Send(new Messages.LogOnReplyMessage() { UserID = userID, IsSeeAll = m_user.IsSeeAll });

			m_user.Init(this, m_world);
		}

		void HandleLogOutMessage(LogOutRequestMessage msg)
		{
			trace.TraceInformation("HandleLogOutMessage");

			m_user.UnInit();

			Send(new Messages.LogOutReplyMessage());

			m_user = null;
			m_userLoggedIn = false;

			Disconnect();
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
		}

		public void Send(IEnumerable<ServerMessage> msgs)
		{
			foreach (var msg in msgs)
				Send(msg);
		}
	}
}
