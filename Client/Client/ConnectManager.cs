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
		EmbeddedServer m_server;
		public EmbeddedServer Server { get { return m_server; } }

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
			if (GameData.Data.User != null)
				return;

			var player = new ClientUser();
			player.DisconnectEvent += OnDisconnected;
			player.StateChangedEvent += (state) => prog.Report(state.ToString());

			await player.LogOn("tomba");

			GameData.Data.User = player;

			var controllable = GameData.Data.World.Controllables.FirstOrDefault();
			if (controllable != null && controllable.Environment != null)
			{
				var mapControl = App.MainWindow.MapControl;
				mapControl.IsVisibilityCheckEnabled = !GameData.Data.User.IsSeeAll;
				mapControl.Environment = controllable.Environment;
				mapControl.CenterPos = new System.Windows.Point(controllable.Location.X, controllable.Location.Y);
				mapControl.Z = controllable.Location.Z;

				if (GameData.Data.World.GameMode == GameMode.Adventure)
					App.MainWindow.FocusedObject = controllable;
			}

			if (Program.StartupStopwatch != null)
			{
				Program.StartupStopwatch.Stop();
				Trace.WriteLine(String.Format("Startup {0} ms", Program.StartupStopwatch.ElapsedMilliseconds));
				Program.StartupStopwatch = null;
			}
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
			var player = GameData.Data.User;
			if (player == null)
				return;

			App.MainWindow.MapControl.Environment = null;

			m_disconnectEvent = new AutoResetEvent(false);

			prog.Report("Saving");
			player.SaveEvent += OnGameSaved;
			GameData.Data.User.Send(new SaveRequestMessage());

			await Task.Run(() => m_disconnectEvent.WaitOne());

			player.SaveEvent -= OnGameSaved;
			prog.Report("Logging Out");
			GameData.Data.User.SendLogOut();

			await Task.Run(() => m_disconnectEvent.WaitOne());

			m_disconnectEvent.Dispose();
			m_disconnectEvent = null;

			player.DisconnectEvent -= OnDisconnected;
		}

		void OnGameSaved()
		{
			m_disconnectEvent.Set();
		}

		void OnDisconnected()
		{
			App.MainWindow.MapControl.Environment = null;

			GameData.Data.User.DisconnectEvent -= OnDisconnected;
			GameData.Data.User = null;

			if (m_disconnectEvent != null)
				m_disconnectEvent.Set();
		}
	}
}
