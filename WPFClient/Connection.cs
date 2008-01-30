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

		public void Connect()
		{
			m_clientCallback = new ClientCallback();

			NetTcpBinding binding = new NetTcpBinding();
			binding.Security.Mode = SecurityMode.None;
			binding.Security.Message.ClientCredentialType = MessageCredentialType.None;

			DuplexChannelFactory<IServerService> cf = 
				new DuplexChannelFactory<IServerService>(m_clientCallback,
					binding, "net.tcp://localhost:8000/MyGame/Server");

			m_server = cf.CreateChannel();
		}

		public void Disconnect()
		{
			if(!HasFaulted())
				(m_server as ICommunicationObject).Close();
		}

		public IServerService Server
		{
			get { return m_server; }
		}

		public bool HasFaulted()
		{
			return (m_server as ICommunicationObject).State == CommunicationState.Faulted;
		}

	}
}
