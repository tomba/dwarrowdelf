using SharpDX.Toolkit;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client
{
	public partial class MainWindowWpf : Window
	{
		public MainWindowWpf()
		{
			InitializeComponent();

			this.Closing += MainWindow_Closing;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			mapControl.Start();

			GameData.Data.UserConnected += Data_UserConnected;
			GameData.Data.UserDisconnected += Data_UserDisconnected;
		}

		void Data_UserConnected()
		{
			var data = GameData.Data;

			if (data.GameMode == GameMode.Adventure)
				mapControl.GoTo(data.FocusedObject);
			else
				mapControl.GoTo(data.World.Controllables.First());
		}

		void Data_UserDisconnected()
		{
			mapControl.Environment = null;
		}


		enum CloseStatus
		{
			None,
			ShuttingDown,
			Ready,
		}

		CloseStatus m_closeStatus;

		async void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			switch (m_closeStatus)
			{
				case CloseStatus.None:
					m_closeStatus = CloseStatus.ShuttingDown;

					mapControl.Stop();

					e.Cancel = true;

					try
					{
						var prog = new Progress<string>(str => Trace.TraceInformation(str));
						await GameData.Data.ConnectManager.DisconnectAsync(prog);
						await GameData.Data.ConnectManager.StopServerAsync(prog);
					}
					catch (Exception exc)
					{
						MessageBox.Show(exc.ToString(), "Error closing down");
					}

					m_closeStatus = CloseStatus.Ready;
					await this.Dispatcher.InvokeAsync(Close);

					break;

				case CloseStatus.ShuttingDown:
					e.Cancel = true;
					break;

				case CloseStatus.Ready:
					break;
			}
		}
	}
}
