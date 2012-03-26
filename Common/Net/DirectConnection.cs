using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.Collections.Concurrent;

namespace Dwarrowdelf
{
	public class DirectConnection : MarshalByRefObject, IConnection
	{
		public static bool UseDirectXXX = false;

		DirectConnection m_remoteConnection;

		BlockingCollection<Message> m_msgQueue = new BlockingCollection<Message>();

		public event Action NewMessageEvent;
		public event Action DisconnectEvent;

		public DirectConnection()
		{

		}

		public DirectConnection(DirectConnection remote)
		{
			m_remoteConnection = remote;
			m_remoteConnection.SetRemote(this);
		}

		void SetRemote(DirectConnection remote)
		{
			m_remoteConnection = remote;
		}

		public int SentMessages { get; private set; }
		public int SentBytes { get; private set; }
		public int ReceivedMessages { get; private set; }
		public int ReceivedBytes { get; private set; }

		public bool IsConnected
		{
			get { return m_remoteConnection != null; }
		}

		public Message GetMessage()
		{
			return m_msgQueue.Take();
		}

		public bool TryGetMessage(out Message msg)
		{
			return m_msgQueue.TryTake(out msg);
		}

		void Enqueue(Message msg)
		{
			this.ReceivedMessages++;

			m_msgQueue.Add(msg);

			var ev = this.NewMessageEvent;

			if (ev != null)
				ev();
		}

		public void Send(Message msg)
		{
			this.SentMessages++;

			m_remoteConnection.Enqueue(msg);
		}

		void RemoteDisconnect()
		{
			if (this.DisconnectEvent != null)
				DisconnectEvent();
		}

		public void Disconnect()
		{
			m_remoteConnection.RemoteDisconnect();
			if (this.DisconnectEvent != null)
				DisconnectEvent();
		}

		public static DirectConnection Connect(IGame game)
		{
			var connection = new DirectConnection();

			game.Connect(connection);

			while (connection.m_remoteConnection == null)
			{
				System.Threading.Thread.Sleep(10);
			}

			return connection;
		}
	}

}
