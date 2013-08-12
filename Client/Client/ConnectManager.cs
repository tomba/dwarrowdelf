using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dwarrowdelf.Messages;

namespace Dwarrowdelf.Client
{
	sealed class ConnectManager
	{
		public event Action<ClientUser> UserConnected;

		EmbeddedServer m_server;
		ClientUser m_user;

		public ClientNetStatistics NetStats { get; private set; }

		public ConnectManager()
		{
			this.NetStats = new ClientNetStatistics();
		}

		public async Task StartServerAndConnectAsync(EmbeddedServerOptions options, ConnectionType connectionType, IProgress<string> prog)
		{
			await StartServerAsync(options, prog);

			Exception connectException = null;

			try
			{
				await ConnectPlayerAsync(connectionType, prog);
			}
			catch (Exception exc)
			{
				connectException = exc;
			}

			if (connectException != null)
			{
				await StopServerAsync(prog);

				throw new Exception("Failed to connect", connectException);
			}
		}

		public async Task StartServerAsync(EmbeddedServerOptions options, IProgress<string> prog)
		{
			if (options.ServerMode == EmbeddedServerMode.None || m_server != null)
				return;

			var server = new EmbeddedServer();
			server.StatusChanged += prog.Report;

			prog.Report("Starting Server");

			await server.StartAsync(options);

			m_server = server;
		}

		public async Task ConnectPlayerAsync(ConnectionType connectionType, IProgress<string> prog)
		{
			if (m_user != null)
				return;

			IConnection connection;

			switch (connectionType)
			{
				case ConnectionType.Tcp:
					connection = await TcpConnection.ConnectAsync(this.NetStats);
					break;

				case ConnectionType.Direct:
					connection = DirectConnection.Connect(m_server.Game);
					break;

				case ConnectionType.Pipe:
					connection = PipeConnection.Connect();
					break;

				default:
					throw new Exception();
			}

			var user = new ClientUser(connection);
			user.DisconnectEvent += OnDisconnected;

			await user.LogOn("tomba", prog);

			m_user = user;

			if (this.UserConnected != null)
				this.UserConnected(user);
		}

		public async Task StopServerAsync(IProgress<string> prog)
		{
			if (m_server == null)
				return;

			prog.Report("Stopping Server");

			await m_server.StopAsync();

			m_server = null;
		}

		AutoResetEvent m_disconnectEvent;

		public async Task DisconnectAsync(IProgress<string> prog)
		{
			if (m_user == null)
				return;

			m_disconnectEvent = new AutoResetEvent(false);

			prog.Report("Saving");
			m_user.SaveEvent += OnGameSaved;
			m_user.Send(new SaveRequestMessage());

			await Task.Run(() => m_disconnectEvent.WaitOne());

			m_user.SaveEvent -= OnGameSaved;
			prog.Report("Logging Out");
			m_user.SendLogOut();

			await Task.Run(() => m_disconnectEvent.WaitOne());

			m_disconnectEvent.Dispose();
			m_disconnectEvent = null;
		}

		void OnGameSaved()
		{
			m_disconnectEvent.Set();
		}

		void OnDisconnected()
		{
			m_user.DisconnectEvent -= OnDisconnected;
			m_user = null;

			if (m_disconnectEvent != null)
				m_disconnectEvent.Set();
		}
	}
}
