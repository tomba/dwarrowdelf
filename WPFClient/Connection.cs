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

		public Connection()
		{
			m_clientCallback = new ClientCallback();

#if LOCAL
			NetNamedPipeBinding binding = new NetNamedPipeBinding();
			binding.Security.Mode = NetNamedPipeSecurityMode.None;
			binding.MaxReceivedMessageSize = 2147483647;
			binding.ReaderQuotas.MaxDepth = 2147483647;
			binding.ReaderQuotas.MaxArrayLength = 2147483647;
			binding.ReaderQuotas.MaxBytesPerRead = 2147483647;
			binding.ReaderQuotas.MaxNameTableCharCount = 2147483647;
			binding.ReaderQuotas.MaxStringContentLength = 2147483647;
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
		}

		public void BeginConnect(AsyncCallback callback, object state)
		{
			(m_server as ICommunicationObject).BeginOpen(callback, state);
		}

		public bool Connect()
		{
			MyDebug.WriteLine("connecting to server");

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

		public void DoAction(GameAction action)
		{
			int tid = GetNewTransactionID();
			action.TransactionID = tid;
			this.Server.DoAction(action);
		}

		public CommunicationState CommState
		{
			get { return (m_server as ICommunicationObject).State; }
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
