using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dwarrowdelf.Messages;

namespace Dwarrowdelf.Server
{
	public sealed class User
	{
		public int UserID { get; private set; }
		public Player Player { get; private set; }
		public string Name { get; private set; }

		IConnection m_connection;
		public bool IsConnected { get { return m_connection != null; } }

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.User");

		public event Action<User> DisconnectEvent;

		public User(IConnection connection, int userID, string name)
		{
			m_connection = connection;
			this.UserID = userID;
			this.Name = name;
			trace.Header = String.Format("User({0}/{1})", this.Name, this.UserID);
		}

		public override string ToString()
		{
			return String.Format("User({0}/{1})", this.Name, this.UserID);
		}

		public void SetPlayer(Player player)
		{
			this.Player = player;
			player.ConnectUser(this);
		}

		public void Disconnect()
		{
			if (this.Player != null)
			{
				this.Player.DisconnectUser();
				this.Player = null;
			}

			m_connection.Disconnect();
		}

		void OnDisconnected()
		{
			trace.TraceInformation("OnDisconnected");

			DH.Dispose(ref m_connection);

			//foreach (var c in m_controllables)
			//	UninitControllableVisionTracker(c);
			//
			//m_world.WorldChanged -= HandleWorldChange;
			//m_world.ReportReceived -= HandleReport;
			//
			//this.IsProceedTurnReplyReceived = false;

			if (DisconnectEvent != null)
				DisconnectEvent(this);
		}

		public void PollNewMessages()
		{
			trace.TraceVerbose("PollNewMessages");

			if (this.Player != null)
			{
				Message msg;
				while (m_connection.TryGetMessage(out msg))
					this.Player.OnReceiveMessage(msg);
			}

			if (!m_connection.IsConnected)
			{
				trace.TraceInformation("PollNewMessages, disconnected");

				OnDisconnected();
			}
		}

		public void Send(ClientMessage msg)
		{
			if (m_connection != null)
				m_connection.Send(msg);
		}

	}
}
