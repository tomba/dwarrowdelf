using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using Dwarrowdelf.Client.UI;
using System.Diagnostics;
using Dwarrowdelf.Messages;
using System.Threading;

namespace Dwarrowdelf.Client
{
	sealed class ConnectManager
	{
		EmbeddedServer m_server;
		public EmbeddedServer Server { get { return m_server; } }

		LogOnDialog m_logOnDialog;

		Dispatcher m_dispatcher;

		public ConnectManager(Dispatcher dispatcher)
		{
			m_dispatcher = dispatcher;
		}

		void SetLogOnText(string text, int idx)
		{
			if (m_dispatcher.CheckAccess() == false)
			{
				m_dispatcher.Invoke(new Action<string, int>(SetLogOnText), text, idx);
				return;
			}

			if (m_logOnDialog == null)
			{
				App.MainWindow.IsEnabled = false;

				m_logOnDialog = new LogOnDialog();
				m_logOnDialog.Owner = App.MainWindow;
				if (idx == 0)
					m_logOnDialog.SetText1(text);
				else
					m_logOnDialog.SetText2(text);
				m_logOnDialog.Show();
			}
			else
			{
				if (idx == 0)
					m_logOnDialog.SetText1(text);
				else
					m_logOnDialog.SetText2(text);
			}
		}

		void CloseLoginDialog()
		{
			if (m_dispatcher.CheckAccess() == false)
			{
				m_dispatcher.Invoke(new Action(CloseLoginDialog));
				return;
			}

			if (m_logOnDialog != null)
			{
				m_logOnDialog.Close();
				m_logOnDialog = null;

				App.MainWindow.IsEnabled = true;
				App.MainWindow.Focus();
			}
		}

		public Task StartServerAndConnectPlayer()
		{
			if ((ClientConfig.EmbeddedServer == EmbeddedServerMode.None || m_server != null) && GameData.Data.User != null)
				return Task.Factory.StartNew(() => { });

			return Task.Factory.StartNew(() =>
			{
				try
				{
					StartServerSync();

					try
					{
						ConnectPlayerSync();
					}
					catch
					{
						StopServer();
						throw;
					}
				}
				finally
				{
					CloseLoginDialog();
				}
			}, TaskCreationOptions.LongRunning);
		}

		public Task StartServer()
		{
			if (ClientConfig.EmbeddedServer == EmbeddedServerMode.None || m_server != null)
				return Task.Factory.StartNew(() => { });

			return Task.Factory.StartNew(() =>
			{
				try
				{
					StartServerSync();
				}
				finally
				{
					CloseLoginDialog();
				}
			}, TaskCreationOptions.LongRunning);
		}

		public Task ConnectPlayer()
		{
			if (GameData.Data.User != null)
				return Task.Factory.StartNew(() => { });

			return Task.Factory.StartNew(() =>
			{
				try
				{
					ConnectPlayerSync();
				}
				finally
				{
					CloseLoginDialog();
				}
			}, TaskCreationOptions.LongRunning);
		}

		void StartServerSync()
		{
			if (ClientConfig.EmbeddedServer == EmbeddedServerMode.None)
				return;

			var server = new EmbeddedServer();
			server.StatusChanged += (str) => SetLogOnText(str, 1);

			SetLogOnText("Starting server", 0);

			server.Start();

			m_dispatcher.Invoke(new Action(() =>
			{
				m_server = server;
			}));
		}

		void ConnectPlayerSync()
		{
			var player = new ClientUser();
			player.DisconnectEvent += OnDisconnected;
			player.StateChangedEvent += (state) => SetLogOnText(state.ToString(), 0);

			player.LogOn("tomba");

			m_dispatcher.Invoke(new Action(() =>
			{
				GameData.Data.User = player;

				var controllable = GameData.Data.World.Controllables.FirstOrDefault();
				if (controllable != null && controllable.Environment != null)
				{
					var mapControl = App.MainWindow.MapControl;
					mapControl.IsVisibilityCheckEnabled = !GameData.Data.User.IsSeeAll;
					mapControl.Environment = controllable.Environment;
					mapControl.AnimatedCenterPos = new System.Windows.Point(controllable.Location.X, controllable.Location.Y);
					mapControl.Z = controllable.Location.Z;
				}

				if (Program.StartupStopwatch != null)
				{
					Program.StartupStopwatch.Stop();
					Trace.WriteLine(String.Format("Startup {0} ms", Program.StartupStopwatch.ElapsedMilliseconds));
					Program.StartupStopwatch = null;
				}
			}));
		}



		public Task DisconnectAndStop()
		{
			if ((ClientConfig.EmbeddedServer != EmbeddedServerMode.None && m_server == null) && GameData.Data.User == null)
				return Task.Factory.StartNew(() => { });

			return Task.Factory.StartNew(() =>
			{
				try
				{
					DisconnectSync();

					StopServerSync();
				}
				finally
				{
					CloseLoginDialog();
				}
			}, TaskCreationOptions.LongRunning);
		}

		public Task StopServer()
		{
			if (ClientConfig.EmbeddedServer == EmbeddedServerMode.None || m_server == null)
				return Task.Factory.StartNew(() => { });

			return Task.Factory.StartNew(() =>
			{
				try
				{
					StopServerSync();
				}
				finally
				{
					CloseLoginDialog();
				}
			}, TaskCreationOptions.LongRunning);
		}

		public Task Disconnect()
		{
			if (GameData.Data.User == null)
				return Task.Factory.StartNew(() => { });

			return Task.Factory.StartNew(() =>
			{
				try
				{
					DisconnectSync();
				}
				finally
				{
					CloseLoginDialog();
				}
			}, TaskCreationOptions.LongRunning);
		}

		void StopServerSync()
		{
			if (ClientConfig.EmbeddedServer == EmbeddedServerMode.None || m_server == null)
				return;

			SetLogOnText("Stopping server", 0);

			m_server.Stop();
			m_server = null;
		}

		AutoResetEvent m_disconnectEvent;

		void DisconnectSync()
		{
			var player = (ClientUser)m_dispatcher.Invoke(new Func<ClientUser>(() =>
			{
				App.MainWindow.MapControl.Environment = null;
				return GameData.Data.User;
			}));

			if (player == null)
				return;

			m_disconnectEvent = new AutoResetEvent(false);

			m_dispatcher.Invoke(new Action(() =>
			{
				SetLogOnText("Saving", 0);
				ClientSaveManager.SaveEvent += OnGameSaved;
				GameData.Data.User.Send(new SaveRequestMessage());
			}));

			m_disconnectEvent.WaitOne();

			m_dispatcher.Invoke(new Action(() =>
			{
				ClientSaveManager.SaveEvent -= OnGameSaved;
				SetLogOnText("Logging Out", 0);
				GameData.Data.User.SendLogOut();
			}));

			m_disconnectEvent.WaitOne();

			m_disconnectEvent.Dispose();
			m_disconnectEvent = null;

			player.DisconnectEvent -= OnDisconnected;

			CloseLoginDialog();
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
