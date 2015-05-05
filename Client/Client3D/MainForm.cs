using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dwarrowdelf.Client
{
	public partial class MainForm : Form
	{
		public Control GameSurface { get { return gameSurfaceControl; } }

		public MainForm()
		{
			InitializeComponent();

			this.Width = 1024;
			this.Height = 800;
			this.Location = new System.Drawing.Point(300, 0);
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;

			this.Shown += MainForm_Shown;
			this.FormClosing += MainForm_FormClosing;
		}

		async void MainForm_Shown(object sender, EventArgs e)
		{
			if (ClientConfig.AutoConnect)
			{
				//var dlg = App.GameWindow.OpenLogOnDialog();

				try
				{
					var prog = new Progress<string>(str => Trace.TraceInformation(str));

					var options = ClientConfig.EmbeddedServerOptions;

					await GameData.Data.ConnectManager.StartServerAndConnectAsync(options, ClientConfig.ConnectionType, prog);
				}
				catch (Exception exc)
				{
					Trace.TraceError("Failed to autoconnect: {0}", exc.Message);
					//MessageBox.Show(this.MainWindow, exc.ToString(), "Failed to autoconnect");
				}

				//dlg.Close();
			}
		}

		enum CloseStatus
		{
			None,
			ShuttingDown,
			Ready,
		}

		CloseStatus m_closeStatus;

		async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			switch (m_closeStatus)
			{
				case CloseStatus.None:
					m_closeStatus = CloseStatus.ShuttingDown;

					e.Cancel = true;

					try
					{
						var prog = new Progress<string>(str => Trace.TraceInformation(str));
						await GameData.Data.ConnectManager.DisconnectAsync(prog);
						await GameData.Data.ConnectManager.StopServerAsync(prog);
					}
					catch (Exception exc)
					{
						Trace.TraceError("Error closing down: {0}", exc.Message);
					}

					m_closeStatus = CloseStatus.Ready;
					Close();

					break;

				case CloseStatus.ShuttingDown:
					e.Cancel = true;
					break;

				case CloseStatus.Ready:
					break;
			}
		}
	}

	class GameSurfaceControl : RenderControl
	{

	}
}
