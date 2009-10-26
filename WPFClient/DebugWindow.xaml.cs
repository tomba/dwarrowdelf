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
using System.Windows.Shapes;
using System.Diagnostics;

namespace MyGame
{
	public partial class DebugWindow : Window
	{
		public DebugWindow()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			Debug.Assert(GameData.Data.MyTraceListener == null);

			GameData.Data.MyTraceListener = new MyTraceListener();
			GameData.Data.MyTraceListener.TextBox = this.logTextBox;
			if (MyGame.Properties.Settings.Default.DebugClient)
				Debug.Listeners.Add(GameData.Data.MyTraceListener);

			if (System.Windows.Forms.SystemInformation.MonitorCount == 2)
			{
				System.Windows.Forms.Screen screen = null;
				for (int i = 0; i < System.Windows.Forms.Screen.AllScreens.Length; ++i)
				{
					if (System.Windows.Forms.Screen.AllScreens[i] !=
						System.Windows.Forms.Screen.PrimaryScreen)
					{
						screen = System.Windows.Forms.Screen.AllScreens[i];
						break;
					}
				}

				var wa = screen.WorkingArea;
				Rect r = new Rect(wa.Left, wa.Top, wa.Width, wa.Height);

				WindowStartupLocation = WindowStartupLocation.Manual;
				Left = r.Left;
				Top = r.Top;
				Width = r.Width;
				Height = r.Height;
			}
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);

			if (MyGame.Properties.Settings.Default.DebugClient)
				Debug.Listeners.Remove(GameData.Data.MyTraceListener);
			if (App.Current.Server != null)
				App.Current.Server.TraceListener = null;
			GameData.Data.MyTraceListener.TextBox = null;
			var tl = GameData.Data.MyTraceListener;
			GameData.Data.MyTraceListener = null;
			tl.Close();
			tl.Dispose();
		}
	}
}
