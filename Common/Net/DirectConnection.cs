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

		public Message Receive()
		{
			Message msg;

			msg = m_msgQueue.Take();

			return msg;
		}

		Action<Message> m_receiveCallback;
		Action m_disconnectCallback;

		public void Start(Action<Message> receiveCallback, Action disconnectCallback)
		{
			m_receiveCallback = receiveCallback;
			m_disconnectCallback = disconnectCallback;
		}

		void Enqueue(Message msg)
		{
			this.ReceivedMessages++;

			if (m_receiveCallback != null)
				m_receiveCallback(msg);
			else
				m_msgQueue.Add(msg);
		}

		public void Send(Message msg)
		{
			this.SentMessages++;

			m_remoteConnection.Enqueue(msg);
		}

		void RemoteDisconnect()
		{
			if (m_disconnectCallback != null)
				m_disconnectCallback();
		}

		public void Disconnect()
		{
			m_remoteConnection.RemoteDisconnect();
			if (m_disconnectCallback != null)
				m_disconnectCallback();
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
