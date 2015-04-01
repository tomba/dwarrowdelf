using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client.UI
{
	public partial class MainWindowMenuBar : UserControl
	{
		public MainWindowMenuBar()
		{
			InitializeComponent();
		}


		/* FILE */

		private void Quit_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			App.GameWindow.Close();
		}


		/* SERVER */

		async void StartServer_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var dlg = App.GameWindow.OpenLogOnDialog();

			try
			{
				var prog = new Progress<string>(str => dlg.AppendText(str));

				var path = Win32.SavedGamesFolder.GetSavedGamesPath();
				path = System.IO.Path.Combine(path, "Dwarrowdelf", "save");

				var options = new EmbeddedServerOptions()
				{
					ServerMode = ClientConfig.EmbeddedServerMode,
					NewGameOptions = ClientConfig.NewGameOptions,
					SaveGamePath = path,
					CleanSaveDir = ClientConfig.CleanSaveDir,
				};

				await GameData.Data.ConnectManager.StartServerAsync(options, prog);
			}
			catch (Exception exc)
			{
				MessageBox.Show(Window.GetWindow(this), exc.ToString(), "Start Server Failed");
			}

			dlg.Close();
		}

		async void StopServer_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var dlg = App.GameWindow.OpenLogOnDialog();

			try
			{
				var prog = new Progress<string>(str => dlg.AppendText(str));
				await GameData.Data.ConnectManager.StopServerAsync(prog);
			}
			catch (Exception exc)
			{
				MessageBox.Show(Window.GetWindow(this), exc.ToString(), "Stop Server Failed");
			}

			dlg.Close();
		}

		async void Connect_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var dlg = App.GameWindow.OpenLogOnDialog();

			try
			{
				var prog = new Progress<string>(str => dlg.AppendText(str));
				await GameData.Data.ConnectManager.ConnectPlayerAsync(ClientConfig.ConnectionType, prog);
			}
			catch (Exception exc)
			{
				MessageBox.Show(Window.GetWindow(this), exc.ToString(), "Connect Player Failed");
			}

			dlg.Close();
		}

		async void Disconnect_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var dlg = App.GameWindow.OpenLogOnDialog();

			try
			{
				var prog = new Progress<string>(str => dlg.AppendText(str));
				await GameData.Data.ConnectManager.DisconnectAsync(prog);
			}
			catch (Exception exc)
			{
				MessageBox.Show(Window.GetWindow(this), exc.ToString(), "Disconnect Failed");
			}

			dlg.Close();
		}

		private void Save_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.User == null)
				return;

			var msg = new Dwarrowdelf.Messages.SaveRequestMessage();

			GameData.Data.User.Send(msg);
		}

		private void Load_MenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		/* DEBUG */

		private void GC_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		private void Break_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}

		private void NetStats_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var netWnd = new UI.NetStatWindow();
			netWnd.Owner = App.GameWindow;
			netWnd.Show();
		}

		private void Stats_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var wnd = new UI.StatWindow();
			wnd.Owner = App.GameWindow;
			wnd.Show();
		}

		private void GCDebug_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var wnd = new UI.GCDebugWindow();
			wnd.Owner = App.GameWindow;
			wnd.Show();
		}
	}
}
