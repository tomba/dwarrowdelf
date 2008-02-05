using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace MyGame
{
	class Connection
	{
		ClientCallback m_clientCallback;
		IServerService m_server;
		int m_transactionNumber;

		public bool Connect()
		{
			m_clientCallback = new ClientCallback();

			NetTcpBinding binding = new NetTcpBinding();
			binding.Security.Mode = SecurityMode.None;
			binding.Security.Message.ClientCredentialType = MessageCredentialType.None;

			DuplexChannelFactory<IServerService> cf = 
				new DuplexChannelFactory<IServerService>(m_clientCallback,
					binding, "net.tcp://localhost:8000/MyGame/Server");

			m_server = cf.CreateChannel();

			try
			{
				(m_server as ICommunicationObject).Open();
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Failed to connect");
				MyDebug.WriteLine(e.ToString());
			}

			return (m_server as ICommunicationObject).State == CommunicationState.Opened;
		}

		public void Disconnect()
		{
			if (!HasFaulted())
			{
				try
				{
					(m_server as ICommunicationObject).Close();
				}
				catch (Exception e)
				{
					MyDebug.WriteLine("Failed to disconnect");
					MyDebug.WriteLine(e.ToString());
				}
			}
		}

		public IServerService Server
		{
			get { return m_server; }
		}

		public void DoAction(GameAction action)
		{
			GameData.Data.ActionCollection.Add(action);
			this.Server.DoAction(action);
		}

		public bool HasFaulted()
		{
			return (m_server as ICommunicationObject).State == CommunicationState.Faulted;
		}

		public int GetTransactionID()
		{
			return System.Threading.Interlocked.Increment(ref m_transactionNumber);
		}

	}
}
