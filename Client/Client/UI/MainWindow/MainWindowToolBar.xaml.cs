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
using Dwarrowdelf.Messages;

namespace Dwarrowdelf.Client.UI
{
	/// <summary>
	/// Interaction logic for MainWindowToolBar.xaml
	/// </summary>
	public partial class MainWindowToolBar : UserControl
	{
		public MainWindowToolBar()
		{
			InitializeComponent();
		}

		private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (GameData.Data.User != null)
			{
				GameData.Data.Connection.Send(new SetWorldConfigMessage()
				{
					MinTickTime = TimeSpan.FromMilliseconds(slider.Value),
				});
			}
		}

		private void Connect_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection != null)
				return;

			App.MainWindow.Connect();
		}

		private void Disconnect_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection == null)
				return;

			App.MainWindow.Disconnect();
		}


		private void EnterGame_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.User == null || GameData.Data.User.IsPlayerInGame)
				return;

			App.MainWindow.EnterGame();
		}

		private void ExitGame_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.User == null || !GameData.Data.User.IsPlayerInGame)
				return;

			App.MainWindow.ExitGame();
		}

		private void Save_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection == null)
				return;

			var msg = new SaveRequestMessage();

			GameData.Data.Connection.Send(msg);
		}

		private void Load_Button_Click(object sender, RoutedEventArgs e)
		{

		}

		private void Button_Click_FullScreen(object sender, RoutedEventArgs e)
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


		private void Button_OpenNetStats_Click(object sender, RoutedEventArgs e)
		{
			var netWnd = new UI.NetStatWindow();
			netWnd.Owner = App.MainWindow;
			netWnd.Show();
		}

		private void Button_Click_GC(object sender, RoutedEventArgs e)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		private void Button_Click_Break(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}


		private void Button_Click_EditSymbols(object sender, RoutedEventArgs e)
		{
			var dialog = new Dwarrowdelf.Client.Symbols.SymbolEditorDialog();
			dialog.SymbolDrawingCache = GameData.Data.SymbolDrawingCache;
			dialog.Show();
		}

	}
}
