#define LOCAL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MyGame
{
	public class Server : MarshalByRefObject, IServer
	{
		public void RunServer(bool isEmbedded)
		{
			MyDebug.Prefix = "[Server] ";

			if (isEmbedded)
			{
				TraceListener listener = (TraceListener)AppDomain.CurrentDomain.GetData("DebugTextWriter");
				if (listener != null)
					Debug.Listeners.Add(listener);
			}
			else
			{
				TraceListener listener = new ConsoleTraceListener();
				Debug.Listeners.Add(listener);
			}

			MyDebug.WriteLine("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			ServiceHost serviceHost = new ServiceHost(typeof(ServerService));

#if LOCAL
			NetNamedPipeBinding binding = new NetNamedPipeBinding();
			binding.Security.Mode = NetNamedPipeSecurityMode.None;
			serviceHost.AddServiceEndpoint(typeof(IServerService),
				binding, "net.pipe://localhost/MyGame/Server");
#else
			//NetTcpBinding binding = new NetTcpBinding();
			//binding.Security.Mode = SecurityMode.TransportWithMessageCredential;
			//binding.Security.Mode = SecurityMode.None;
			//binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

			serviceHost.AddServiceEndpoint(typeof(IServerService),
				binding, "net.tcp://localhost:8000/MyGame/Server");

			serviceHost.Credentials.UserNameAuthentication.UserNamePasswordValidationMode =
				System.ServiceModel.Security.UserNamePasswordValidationMode.Custom;
			serviceHost.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator =
				new CustomUserNameValidator();

			/*
			 * makecert -r -pe -n "CN=CompanyXYZ Server" -b 01/01/2007 -e 01/01/2010 -sky exchange Server.cer -sv Server.pvk
			 * pvk2pfx.exe -pvk Server.pvk -spc Server.cer -pfx Server.pfx
			 */
			/*
			X509Certificate2 cert = new X509Certificate2("Server.pfx");
			serviceHost.Credentials.ServiceCertificate.Certificate = cert;
			*/
#endif

			EventWaitHandle serverWaitHandle =
				new EventWaitHandle(false, EventResetMode.AutoReset, "MyGame.ServerWaitHandle");

			try
			{
				serviceHost.Open();

				MyDebug.WriteLine("The service is ready.");

				if (isEmbedded)
				{
					MyDebug.WriteLine("Server signaling client for start.");
					serverWaitHandle.Set();
					serverWaitHandle.WaitOne();
				}
				else
				{
					Console.WriteLine("Press enter to exit");
					Console.ReadLine();
				}

				MyDebug.WriteLine("Server exiting");

				serviceHost.Close();
			}
			catch (CommunicationException ce)
			{
				MyDebug.WriteLine("An exception occurred: {0}", ce.Message);
				serviceHost.Abort();
			}

			MyDebug.WriteLine("Server exit");
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MyDebug.WriteLine("tuli exc");

		}

	}

	public class CustomUserNameValidator : System.IdentityModel.Selectors.UserNamePasswordValidator
	{
		public override void Validate(string userName, string password)
		{
			MyDebug.WriteLine("Validate {0}, {1}", userName, password);

			if (null == userName || null == password)
			{
				throw new ArgumentNullException();
			}

			if (userName != "tomba")
				throw new System.IdentityModel.Tokens.SecurityTokenValidationException("illegal user");

		}
	}
}
