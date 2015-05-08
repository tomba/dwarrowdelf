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
		MyGame game;

		public MainWindowWpf()
		{
			InitializeComponent();

			this.Closing += MainWindow_Closing;
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			InitSharpDXSurface();
		}

		void InitSharpDXSurface()
		{
			/*
				* SendResizeToGame: true - sends resize event to the game when the element is resized with the 'SendResizeDelay' delay
				* ReceiveResizeFromGame: true - the size of the element will be controller by the game
				* SendResizeDelay: 0.5 seconds - wait this time after last resize event before sending resize to the game
				* LowPriorityRendering: false - when set to true - executes the game loop with the DispatcherPriority.Input, which allows normal processing of input events, may skip some frames
				*/
			var elem = new SharpDXElement()
			{
				LowPriorityRendering = true,
				SendResizeToGame = true,
				SendResizeDelay = TimeSpan.FromSeconds(0.5),
			};

			gameGrid.Children.Add(elem);

			game = new MyGame();
			game.Run(elem);
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
