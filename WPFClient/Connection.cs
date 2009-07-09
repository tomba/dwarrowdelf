#define LOCAL

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
			MyDebug.WriteLine("connecting to server");

			m_clientCallback = new ClientCallback();

#if LOCAL
			NetNamedPipeBinding binding = new NetNamedPipeBinding();
			binding.Security.Mode = NetNamedPipeSecurityMode.None;
			EndpointAddress ea = new EndpointAddress(new Uri("net.pipe://localhost/MyGame/Server"),
				EndpointIdentity.CreateDnsIdentity("CompanyXYZ Server"));
#else
			//NetTcpBinding binding = new NetTcpBinding();
			//binding.Security.Mode = SecurityMode.TransportWithMessageCredential;
			//binding.Security.Mode = SecurityMode.None;
			//binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
			
			EndpointAddress ea = new EndpointAddress(new Uri("net.tcp://localhost:8000/MyGame/Server"),
				EndpointIdentity.CreateDnsIdentity("CompanyXYZ Server"));
#endif

			DuplexChannelFactory<IServerService> cf = 
				new DuplexChannelFactory<IServerService>(m_clientCallback,
					binding, ea);

			cf.Credentials.ServiceCertificate.Authentication.CertificateValidationMode =
				System.ServiceModel.Security.X509CertificateValidationMode.None;
			cf.Credentials.UserName.UserName = "tomba";
			cf.Credentials.UserName.Password = "passu";

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

			MyDebug.WriteLine("connect done");

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

		public int GetNewTransactionID()
		{
			return System.Threading.Interlocked.Increment(ref m_transactionNumber);
		}

	}
}
