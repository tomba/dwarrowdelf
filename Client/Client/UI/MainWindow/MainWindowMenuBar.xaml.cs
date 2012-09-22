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
			App.MainWindow.Close();
		}


		/* SERVER */

		async void StartServer_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				await GameData.Data.ConnectManager.StartServerAsync();
			}
			catch (Exception exc)
			{
				MessageBox.Show(Window.GetWindow(this), exc.ToString(), "Start Server Failed");
			}
		}

		async void StopServer_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				await GameData.Data.ConnectManager.StopServerAsync();
			}
			catch (Exception exc)
			{
				MessageBox.Show(Window.GetWindow(this), exc.ToString(), "Stop Server Failed");
			}
		}

		async void Connect_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				await GameData.Data.ConnectManager.ConnectPlayerAsync();
			}
			catch (Exception exc)
			{
				MessageBox.Show(Window.GetWindow(this), exc.ToString(), "Connect Player Failed");
			}
		}

		async void Disconnect_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				await GameData.Data.ConnectManager.DisconnectAsync();
			}
			catch (Exception exc)
			{
				MessageBox.Show(Window.GetWindow(this), exc.ToString(), "Disconnect Failed");
			}
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
			netWnd.Owner = App.MainWindow;
			netWnd.Show();
		}


		/* Window */

		private void FullScreen_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var button = (System.Windows.Controls.Primitives.ToggleButton)sender;

			var wnd = App.MainWindow;

			if (button.IsChecked.Value)
			{
				wnd.WindowStyle = System.Windows.WindowStyle.None;
				wnd.Topmost = true;
				wnd.WindowState = System.Windows.WindowState.Maximized;
			}
			else
			{
				wnd.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
				wnd.Topmost = false;
				wnd.WindowState = System.Windows.WindowState.Normal;
			}
		}


	}
}
