using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.Threading;
using System.Diagnostics;

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
				Debug.Listeners.Add(listener);
			}
			else
			{
				TraceListener listener = new ConsoleTraceListener();
				Debug.Listeners.Add(listener);
			}

			MyDebug.WriteLine("Server starting");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			ServiceHost serviceHost = new ServiceHost(typeof(ServerService));
			
			NetTcpBinding binding = new NetTcpBinding();
			binding.Security.Mode = SecurityMode.None;
			binding.Security.Message.ClientCredentialType = MessageCredentialType.None;

			serviceHost.AddServiceEndpoint(typeof(IServerService),
				binding, "net.tcp://localhost:8000/MyGame/Server");

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
}
