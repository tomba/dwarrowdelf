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

		SymbolBitmapCache m_bitmapCache;

		DispatcherTimer m_updateTimer;
		Location m_screenToMapDelta;

		Location m_center;
		ClientGameObject m_followObject;
		MapLevel m_mapLevel;

		public MainWindow()
		{
			s_mainWindow = this;

			InitializeComponent();

			this.Width = 1024;
			this.Height = 600;

			m_bitmapCache = new SymbolBitmapCache();
			m_bitmapCache.SymbolDrawings = GameData.Data.SymbolDrawings.Drawings;

			map.KeyDown += MapControl_KeyDown;
			map.MouseDown += new MouseButtonEventHandler(MapControl_MouseDown);
			map.DimensionsChangedEvent += MapControl_DimensionsChanged;

			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(30);
		}
		protected override void OnInitialized(EventArgs e)
		{

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
					m_bitmapCache.TileSize = map.TileSize;
					return;

				case Key.Subtract:
					e.Handled = true;
					if (map.TileSize <= 10)
						return;
					map.TileSize -= 10;
					m_bitmapCache.TileSize = map.TileSize;
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
			//this.Focus();
			//MyDebug.WriteLine("Mouse down");

			if (e.RightButton == MouseButtonState.Pressed)
			{
				int x, y;
				map.TileFromPoint(e.GetPosition(map), out x, out y);
				Location sl = new Location(x, y);
				Location ml = ScreenToMap(sl);

				if (!m_mapLevel.Bounds.Contains(ml))
					return;

				GameData.Data.Connection.Server.ToggleTile(ml);

				e.Handled = true;
			}
		}

		void MapControl_DimensionsChanged()
		{
			UpdateScreenToMapDelta();
			UpdateMap();
		}

		void UpdateScreenToMapDelta()
		{
			int dx = m_center.X - this.map.Columns / 2;
			int dy = m_center.Y - this.map.Rows / 2;
			m_screenToMapDelta = new Location(dx, dy);
		}

		public void UpdateMap()
		{
			if (!m_updateTimer.IsEnabled)
				m_updateTimer.Start();
		}

		void UpdateTimerTick(object sender, EventArgs e)
		{
			m_updateTimer.Stop();

			// XXX update all for now. this may be ok anyway, LOS etc changes quite a lot of the screen
			PopulateMapTiles(); 
		}

		void PopulateMapTiles()
		{
			for (int y = 0; y < map.Rows; y++)
				for (int x = 0; x < map.Columns; x++)
					UpdateTile(x, y);
		}

		void UpdateTile(int x, int y)
		{
			BitmapSource bmp = null;
			MapTile tile;

			tile = map.GetTile(x, y);

			bmp = GetBitmap(x, y);
			tile.Bitmap = bmp;

			bmp = GetObjectBitmap(x, y);
			tile.ObjectBitmap = bmp;
		}

		BitmapSource GetBitmap(int x, int y)
		{
			if (this.Map == null)
				return null;

			Location ml = new Location(x, y) + m_screenToMapDelta;

			int terrainID = this.Map.GetTerrainType(ml);
			return m_bitmapCache.GetBitmap(terrainID, false);
		}

		BitmapSource GetObjectBitmap(int x, int y)
		{
			if (this.Map == null)
				return null;

			Location ml = new Location(x, y) + m_screenToMapDelta;

			IList<ClientGameObject> obs = this.Map.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				int id = obs[0].SymbolID;
				return m_bitmapCache.GetBitmap(id, false);
			}
			else
				return null;
		}

		internal MapLevel Map
		{
			get { return m_mapLevel; }

			set
			{
				if (m_mapLevel != null)
					m_mapLevel.MapChanged -= MapChangedCallback;
				m_mapLevel = value;
				m_mapLevel.MapChanged += new MapChanged(MapChangedCallback);
				UpdateMap();
			}
		}

		void MapChangedCallback(Location l)
		{
			UpdateMap();
		}

		internal ClientGameObject FollowObject
		{
			get
			{
				return m_followObject;
			}

			set
			{
				if (m_followObject != null)
					m_followObject.ObjectMoved -= FollowedObjectMoved;
				m_followObject = value;
				m_followObject.ObjectMoved += FollowedObjectMoved;

				if (m_followObject.Environment != null)
					FollowedObjectMoved(m_followObject.Environment, m_followObject.Location);
			}

		}

		void FollowedObjectMoved(MapLevel e, Location l)
		{
			if (e != m_mapLevel)
			{
				this.Map = m_mapLevel;
				m_center = new Location(-1, -1);
			}

			int xd = map.Columns / 2;
			int yd = map.Rows / 2;
			Location newCenter = new Location(((l.X+xd/2) / xd) * xd, ((l.Y+yd/2) / yd) * yd);

			if (m_center != newCenter)
			{
				m_center = newCenter;
				UpdateScreenToMapDelta();

				UpdateMap();
			}

			//Canvas.SetLeft(m_hiliteRectangle, MapToScreen(l).X * m_tileSize);
			//Canvas.SetTop(m_hiliteRectangle, MapToScreen(l).Y * m_tileSize);

			//MyDebug.WriteLine(String.Format("FollowedObjectMoved {0}, center {1}", l, m_center));
		}

		public Location ScreenToMap(Location sl)
		{
			return sl + m_screenToMapDelta;
		}

		public Location MapToScreen(Location ml)
		{
			return ml - m_screenToMapDelta;
		}

		private void OnClearLogClicked(object sender, RoutedEventArgs e)
		{
			this.logTextBox.Clear();
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1)
			{
				this.FollowObject = null;
				return;
			}

			ClientGameObject ob = (ClientGameObject)e.AddedItems[0];

			this.FollowObject = ob;
		}
	}
}
