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
		User m_user;

		System.Collections.Concurrent.ConcurrentQueue<Message> m_msgQueue = new System.Collections.Concurrent.ConcurrentQueue<Message>();

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		public ServerConnection(IConnection connection)
		{
			m_connection = connection;

			trace.Header = "ServerConnection";
			trace.TraceInformation("New ServerConnection");

			m_connection.ReceiveEvent += OnReceiveMessage;
			m_connection.DisconnectEvent += _OnDisconnect;
			m_connection.BeginRead();
		}

		public void Init(World world)
		{
			m_world = world;
			m_world.AddConnection(this);
		}

		public void Disconnect()
		{
			trace.TraceInformation("Disconnect");

			if (m_userLoggedIn)
			{
				m_user.OnDisconnected();
				m_user = null;
				m_userLoggedIn = false;
			}

			m_world.RemoveConnection(this);

			m_world = null;

			m_connection.ReceiveEvent -= OnReceiveMessage;
			m_connection.DisconnectEvent -= _OnDisconnect;
			m_connection = null;
		}

		void _OnDisconnect()
		{
			trace.TraceInformation("_OnDisconnect");
			m_world.BeginInvokeInstant(new Action(Disconnect), null);
		}

		void OnReceiveMessage(Message m)
		{
			trace.TraceVerbose("OnReceiveMessage");

			m_msgQueue.Enqueue(m);
			m_world.SignalWorld();
		}

		public void HandleNewMessages()
		{
			var count = m_msgQueue.Count;

			trace.TraceVerbose("HandleNewMessages, count = {0}", count);

			if (count == 0)
				return;

			if (m_userLoggedIn == false)
				HandleLoginMessage();

			Message msg;
			while (m_msgQueue.TryDequeue(out msg))
				m_user.OnReceiveMessage(msg);
		}

		void HandleLoginMessage()
		{
			trace.TraceInformation("HandleLoginMessage");

			Message m;
			bool ok = m_msgQueue.TryDequeue(out m);

			Debug.Assert(ok);

			LogOnRequestMessage msg = m as LogOnRequestMessage;
			if (msg == null)
				throw new Exception();

			string name = msg.Name;

			trace.Header = String.Format("ServerConnection({0})", name);
			trace.TraceInformation("LogOnRequestMessage");

			var userID = s_userIDs++;
			m_userLoggedIn = true;

			m_user = new User(userID);

			m_connection.Send(new Messages.LogOnReplyMessage() { UserID = userID, IsSeeAll = m_user.IsSeeAll });

			m_user.Init(this, m_world);
		}

		public void Send(ServerMessage msg)
		{
			m_connection.Send(msg);
		}

		public void Send(IEnumerable<ServerMessage> msgs)
		{
			foreach (var msg in msgs)
				Send(msg);
		}
	}
}
