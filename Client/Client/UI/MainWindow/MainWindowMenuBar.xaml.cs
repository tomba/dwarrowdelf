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

		private void StartServer_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var task = GameData.Data.ConnectManager.StartServer();
			task.ContinueWith((t) =>
			{
				MessageBox.Show(Window.GetWindow(this), t.Exception.ToString(), "Start Server Failed");
			}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private void StopServer_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var task = GameData.Data.ConnectManager.StopServer();
			task.ContinueWith((t) =>
			{
				MessageBox.Show(Window.GetWindow(this), t.Exception.ToString(), "Stop Server Failed");
			}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private void Connect_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var task = GameData.Data.ConnectManager.ConnectPlayer();
			task.ContinueWith((t) =>
			{
				MessageBox.Show(Window.GetWindow(this), t.Exception.ToString(), "Connect Player Failed");
			}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private void Disconnect_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var task = GameData.Data.ConnectManager.Disconnect();
			task.ContinueWith((t) =>
			{
				MessageBox.Show(Window.GetWindow(this), t.Exception.ToString(), "Disconnect Failed");
			}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
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
