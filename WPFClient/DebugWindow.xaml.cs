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

namespace MyGame
{
	public partial class DebugWindow : Window
	{
		public DebugWindow()
		{
			InitializeComponent();

			if (System.Windows.Forms.SystemInformation.MonitorCount == 2)
			{
				var screen = System.Windows.Forms.Screen.AllScreens[1];
				var wa = screen.WorkingArea;
				Rect r = new Rect(wa.Left, wa.Top, wa.Width, wa.Height);

				WindowStartupLocation = WindowStartupLocation.Manual;
				Left = r.Left;
				Top = r.Top;
				Width = r.Width;
				Height = r.Height;
			}
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			if (GameData.Data.MyTraceListener != null)
				GameData.Data.MyTraceListener.TextBox = this.logTextBox;
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);

			if (GameData.Data.MyTraceListener != null)
				GameData.Data.MyTraceListener.TextBox = null;
		}
	}
}
