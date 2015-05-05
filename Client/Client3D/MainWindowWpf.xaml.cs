using SharpDX.Toolkit;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
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
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			object gameSurface;

			if (Program.Mode == ThreeDMode.WpfSharpDXElement)
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
				gameSurface = elem;
			}
			else if (Program.Mode == ThreeDMode.WpfHwndHost)
			{
				var elem = new Border()
				{
				};
				gameGrid.Children.Add(elem);
				gameSurface = elem;
			}
			else
			{
				throw new NotImplementedException();
			}

			game = new MyGame();
			game.Run(gameSurface);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			if (Program.Mode == ThreeDMode.WpfHwndHost)
				FixSharpDXBug();
		}

		void FixSharpDXBug()
		{
			// XXX fix SharpDX bug. Without this, the renderform will be sized wrongly, and will get resized constantly
			var wnd = game.Window;
			var nwnd = (RenderForm)wnd.NativeWindow;

			nwnd.Invoke(new Action(() =>
			{
				nwnd.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			}));
		}
	}
}
