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
using System.Windows.Threading;

namespace MyGame
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	partial class MainWindow : Window
	{
		public static MainWindow s_mainWindow; // xxx

		public MainWindow()
		{
			s_mainWindow = this;
			this.WindowState = WindowState.Maximized;

			InitializeComponent();

			this.Width = 1024;
			this.Height = 600;

			map.KeyDown += MapControl_KeyDown;
			map.MouseDown += MapControl_MouseDown;
		}

		protected override void OnInitialized(EventArgs e)
		{
			map.ContextMenu = null;

			base.OnInitialized(e);

			GameData.Data.MyTraceListener.TextBox = this.logTextBox;

			map.Focus();
		}

		void MapControl_KeyDown(object sender, KeyEventArgs e)
		{
			//MyDebug.WriteLine("OnMyKeyDown");

			Direction dir;
			switch (e.Key)
			{
				case Key.Up: dir = Direction.North; break;
				case Key.Down: dir = Direction.South; break;
				case Key.Left: dir = Direction.West; break;
				case Key.Right: dir = Direction.East; break;
				case Key.Home: dir = Direction.NorthWest; break;
				case Key.End: dir = Direction.SouthWest; break;
				case Key.PageUp: dir = Direction.NorthEast; break;
				case Key.PageDown: dir = Direction.SouthEast; break;

				case Key.Space:
					{
						e.Handled = true;
						int wtid = GameData.Data.Connection.GetNewTransactionID();
						GameData.Data.Connection.DoAction(new WaitAction(wtid, GameData.Data.Player, 1));
						return;
					}

				case Key.Add:
					e.Handled = true;
					map.TileSize += 10;
					return;

				case Key.Subtract:
					e.Handled = true;
					if (map.TileSize <= 10)
						return;
					map.TileSize -= 10;
					return;

				default:
					return;
			}

			e.Handled = true;
			int tid = GameData.Data.Connection.GetNewTransactionID();
			GameData.Data.Connection.DoAction(new MoveAction(tid, GameData.Data.Player, dir));
		}

		void MapControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
	//		Location ml = map.MapLocationFromPoint(e.GetPosition(map));

		//	map.Selection = new MapControlBase.TileSelection() { Start = ml, End = ml };

			return;
			map.Focus();
			//MyDebug.WriteLine("Mouse down");
			/*
			if (e.RightButton == MouseButtonState.Pressed)
			{
				Location ml = map.MapLocationFromPoint(e.GetPosition(map));

				if (!map.Map.Bounds.Contains(ml))
					return;

				GameData.Data.Connection.Server.ToggleTile(ml);

				e.Handled = true;
			}*/
		}

		private void OnClearLogClicked(object sender, RoutedEventArgs e)
		{
			this.logTextBox.Clear();
		}

		internal MapLevel Map
		{
			get { return map.Map; }
			set { map.Map = value; }
		}
	}
}
