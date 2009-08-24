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
			Application.Current.MainWindow = this;
			this.WindowState = WindowState.Maximized;

			InitializeComponent();

			this.Width = 1024;
			this.Height = 600;

			map.KeyDown += MapControl_KeyDown;
			map.MouseDown += MapControl_MouseDown;
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			map.Focus();
		}

		void MapControl_KeyDown(object sender, KeyEventArgs e)
		{
			//MyDebug.WriteLine("OnMyKeyDown");
			if (GameData.Data.Connection == null)
				return;

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
					if (map.TileSize <= 16)
						return;
					map.TileSize -= 8;
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
			if (e.RightButton == MouseButtonState.Pressed)
			{
				IntRect r = map.SelectionRect;

				if (r.Width > 1 || r.Height > 1)
					return;

				IntPoint ml = map.MapLocationFromPoint(e.GetPosition(map));

				map.SelectionRect = new IntRect(ml, new IntSize(1, 1));
			}
		}

		private void OnClearLogClicked(object sender, RoutedEventArgs e)
		{
			App.s_debugWindow.logTextBox.Clear();
		}

		internal Environment Map
		{
			get { return map.Map; }
			set { map.Map = value; }
		}

		private void MenuItem_Click_Floor(object sender, RoutedEventArgs e)
		{
			var terrain = this.Map.World.AreaData.Terrains.Single(t => t.Name == "Dungeon Floor");
			IntRect r = map.SelectionRect;
			GameData.Data.Connection.Server.SetTiles(r, terrain.ID);
		}

		private void MenuItem_Click_Wall(object sender, RoutedEventArgs e)
		{
			var terrain = this.Map.World.AreaData.Terrains.Single(t => t.Name == "Dungeon Wall");
			IntRect r = map.SelectionRect;
			GameData.Data.Connection.Server.SetTiles(r, terrain.ID);
		}
	}
}
