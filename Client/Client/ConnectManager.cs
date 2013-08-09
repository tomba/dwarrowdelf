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
		public IGame Game { get { return m_server != null ? m_server.Game : null; } }

		public event Action<ClientUser> UserConnected;

		EmbeddedServer m_server;
		ClientUser m_user;

		public ConnectManager()
		{
		}

		public async Task StartServerAndConnectAsync(EmbeddedServerMode mode, GameMode newGameMode, bool cleanSaveDir, IProgress<string> prog)
		{
			await StartServerAsync(mode, newGameMode, cleanSaveDir, prog);

			Exception connectException = null;

			try
			{
				await ConnectPlayerAsync(prog);
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

		public async Task StartServerAsync(EmbeddedServerMode mode, GameMode newGameMode, bool cleanSaveDir, IProgress<string> prog)
		{
			if (mode == EmbeddedServerMode.None || m_server != null)
				return;

			var server = new EmbeddedServer();
			server.StatusChanged += prog.Report;

			prog.Report("Starting Server");

			var path = Win32.SavedGamesFolder.GetSavedGamesPath();
			path = System.IO.Path.Combine(path, "Dwarrowdelf", "save");

			await server.StartAsync(mode, path, cleanSaveDir, newGameMode);

			m_server = server;
		}

		public async Task ConnectPlayerAsync(IProgress<string> prog)
		{
			if (m_user != null)
				return;

			var player = new ClientUser();
			player.DisconnectEvent += OnDisconnected;
			player.StateChangedEvent += (state) => prog.Report(state.ToString());

			await player.LogOn("tomba");

			m_user = player;

			if (this.UserConnected != null)
				this.UserConnected(player);
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
