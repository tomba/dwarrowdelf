using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

using System.Windows.Resources;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;


namespace Dwarrowdelf.Client
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public new static App Current { get { return (App)Application.Current; } }
		internal new static MainWindow MainWindow { get { return (MainWindow)Application.Current.MainWindow; } }

		EventWaitHandle m_serverStartWaitHandle;
		RegisteredWaitHandle m_registeredWaitHandle;
		bool m_serverInAppDomain;

		EmbeddedServer m_embeddedServer;

		Window m_serverStartDialog; // Hacky dialog
		Label m_serverDialogLabel;

		protected override void OnStartup(StartupEventArgs e)
		{
			m_serverInAppDomain = true;

			Thread.CurrentThread.Name = "Main";

			base.OnStartup(e);

			Trace.TraceInformation("Start");

			int magic = 0;
			GameAction.MagicNumberGenerator = () => Math.Abs(Interlocked.Increment(ref magic));

			if (m_serverInAppDomain)
			{
				string gameDir = "save";
				bool cleanSaves = true;
				string saveFile = null;

				if (!Directory.Exists(gameDir))
					Directory.CreateDirectory(gameDir);

				if (cleanSaves)
				{
					var files = Directory.EnumerateFiles(gameDir);
					foreach (var file in files)
						File.Delete(file);
				}

				if (!cleanSaves)
				{
					saveFile = GetLatestSaveFile(gameDir);
				}

				m_serverStartWaitHandle = new AutoResetEvent(false);

				m_registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(m_serverStartWaitHandle,
					ServerStartedCallback, null, TimeSpan.FromMinutes(1), true);

				m_serverStartDialog = new Window();
				m_serverStartDialog.Topmost = true;
				m_serverStartDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
				m_serverStartDialog.Width = 200;
				m_serverStartDialog.Height = 200;
				m_serverDialogLabel = new Label();
				m_serverDialogLabel.Content = "Starting Server";
				m_serverStartDialog.Content = m_serverDialogLabel;
				m_serverStartDialog.Show();

				m_embeddedServer = new EmbeddedServer(saveFile);
				m_embeddedServer.ServerStatusChangeEvent += OnServerStatusChange;
				m_embeddedServer.Start(m_serverStartWaitHandle);
			}
		}

		static string GetLatestSaveFile(string gameDir)
		{
			var files = Directory.EnumerateFiles(gameDir);
			var list = new System.Collections.Generic.List<string>(files);
			list.Sort();
			var last = list[list.Count - 1];
			return Path.GetFileName(last);
		}

		void OnServerStatusChange(string status)
		{
			this.Dispatcher.BeginInvoke(new Action(delegate { m_serverDialogLabel.Content = status; }));
		}

		void ServerStartedCallback(object state, bool timedOut)
		{
			if (timedOut)
				throw new Exception();

			m_registeredWaitHandle.Unregister(m_serverStartWaitHandle);
			m_registeredWaitHandle = null;
			m_serverStartWaitHandle.Close();
			m_serverStartWaitHandle = null;

			// XXX mainwindow is already open before server is up
			this.Dispatcher.BeginInvoke(new Action(delegate
			{
				m_serverStartDialog.Close();
				MainWindow.OnServerStarted();
			}));
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			if (m_serverInAppDomain)
				m_embeddedServer.Stop();

			Debug.Print("Exiting");
		}

		class EmbeddedServer
		{
			IGame m_game;
			Thread m_serverThread;
			EventWaitHandle m_serverStartWaitHandle;
			AppDomain m_serverDomain;
			string m_saveFile;

			public Action<string> ServerStatusChangeEvent;

			public EmbeddedServer(string saveFile)
			{
				m_saveFile = saveFile;

				var di = AppDomain.CurrentDomain.SetupInformation;

				var domainSetup = new AppDomainSetup()
				{
					ApplicationBase = di.ApplicationBase,
					ConfigurationFile = di.ApplicationBase + "Server.exe.config",
				};

				m_serverDomain = AppDomain.CreateDomain("ServerDomain", null, domainSetup);

				string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
				var baseDir = System.IO.Path.GetDirectoryName(exePath);

				var serverPath = System.IO.Path.Combine(baseDir, "Dwarrowdelf.Server.Engine.dll");

				UpdateStatus("Creating Game");

				var gameFactory = (IGameFactory)m_serverDomain.CreateInstanceFromAndUnwrap(serverPath, "Dwarrowdelf.Server.GameFactory");

				m_game = gameFactory.CreateGame("MyArea.dll", "save");

				UpdateStatus("Game Created");
			}

			void UpdateStatus(string status)
			{
				if (ServerStatusChangeEvent != null)
					ServerStatusChangeEvent(status);
			}

			public void Start(EventWaitHandle serverStartWaitHandle)
			{
				m_serverStartWaitHandle = serverStartWaitHandle;

				m_serverThread = new Thread(Main);
				m_serverThread.Start();
			}

			public void Stop()
			{
				m_game.Stop();
				m_serverThread.Join();
			}

			void Main()
			{
				Thread.CurrentThread.Priority = ThreadPriority.Lowest;
				Thread.CurrentThread.Name = "Main";

				if (m_saveFile == null)
				{
					UpdateStatus("Creating World");
					m_game.CreateWorld();
					UpdateStatus("World Created");
				}
				else
				{
					UpdateStatus("Loading World");
					m_game.LoadWorld(m_saveFile);
					UpdateStatus("World Loaded");
				}

				UpdateStatus("Starting Game");

				m_game.Run(m_serverStartWaitHandle);

				AppDomain.Unload(m_serverDomain);
			}
		}
	}
}
