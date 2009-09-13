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
		TraceListener m_traceListener;

		public Server()
		{
			MyDebug.Prefix = "[Server] ";
		}

		public TraceListener TraceListener 
		{ 
			set 
			{
				if (value != null)
				{
					Debug.Assert(m_traceListener == null);
					m_traceListener = value;
					Debug.Listeners.Add(value);
				}
				else
				{
					if (m_traceListener != null)
						Debug.Listeners.Remove(m_traceListener);
					m_traceListener = null;
				}
			}
		}

		public void RunServer(bool isEmbedded,
			EventWaitHandle serverStartWaitHandle, EventWaitHandle serverStopWaitHandle)
		{
			MyDebug.WriteLine("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			/* Load area */

			IAreaData areaData = new MyAreaData.AreaData();
			IArea area = new MyArea.Area();

			var world = new World(area, areaData);
			area.InitializeWorld(world);
			world.StartWorld();

			World.TheWorld = world;

			/* WCF */

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

			//EventWaitHandle serverWaitHandle = EventWaitHandle.OpenExisting("MyGame.ServerWaitHandle");

			try
			{
				serviceHost.Open();

				MyDebug.WriteLine("The service is ready.");

				if (isEmbedded)
				{
					MyDebug.WriteLine("Server signaling client for start.");
					if (serverStartWaitHandle != null)
					{
						serverStartWaitHandle.Set();
						serverStopWaitHandle.WaitOne();
					}
				}
				else
				{
					Console.WriteLine("Press enter to exit");
					while (Console.ReadKey().Key != ConsoleKey.Enter)
						world.SignalWorld();
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
