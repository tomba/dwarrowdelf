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
using System.ComponentModel;

namespace MyGame
{
	class MapControl : UserControl, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		SymbolBitmapCache m_bitmapCache;

		DispatcherTimer m_updateTimer;
		Location m_screenToMapDelta;

		Location m_center;
		ClientGameObject m_followObject;
		MapLevel m_mapLevel;

		int m_tileSize;
		MapControlBase map;

		Canvas m_effectsCanvas = new Canvas();
		Rectangle m_hiliteRectangle = new Rectangle();

		public MapControl()
		{
			m_bitmapCache = new SymbolBitmapCache();
			m_bitmapCache.SymbolDrawings = GameData.Data.SymbolDrawings.Drawings;

			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(30);

			Grid grid = new Grid();
			this.AddChild(grid);

			map = new MapControlBase();
			map.DimensionsChangedEvent += MapControl_DimensionsChanged;
			grid.Children.Add(map);

			this.TileSize = 40;

			grid.Children.Add(m_effectsCanvas);

			m_hiliteRectangle.Width = m_tileSize;
			m_hiliteRectangle.Height = m_tileSize;
			m_hiliteRectangle.Stroke = Brushes.Blue;
			m_hiliteRectangle.StrokeThickness = 2;
			m_effectsCanvas.Children.Add(m_hiliteRectangle);

			this.Focusable = true;
		}

		public int TileSize
		{
			get { return m_tileSize; }

			set
			{
				m_tileSize = value;

				map.TileSize = value;
				m_bitmapCache.TileSize = value;

				m_hiliteRectangle.Width = m_tileSize;
				m_hiliteRectangle.Height = m_tileSize;
			}
		}

		void MapControl_DimensionsChanged()
		{
			UpdateScreenToMapDelta();
			UpdateMap();
		}

		void UpdateScreenToMapDelta()
		{
			int dx = m_center.X - map.Columns / 2;
			int dy = m_center.Y - map.Rows / 2;
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
			if (m_followObject != null)
				m_followObject.UpdateVisibilityMap();

			for (int y = 0; y < map.Rows; y++)
				for (int x = 0; x < map.Columns; x++)
					UpdateTile(new Location(x, y));
		}

		void UpdateTile(Location sl)
		{
			BitmapSource bmp = null;
			MapTile tile;
			bool lit = false;
			Location ml = sl + m_screenToMapDelta;

			tile = map.GetTile(sl);

			if (m_mapLevel == null) // || !m_mapLevel.Bounds.Contains(ml))
			{
				tile.Bitmap = null;
				tile.ObjectBitmap = null;
				return;
			}

			if (m_followObject != null)
			{
				if (m_mapLevel.GetTerrainType(ml) == 0)
				{
					// unknown locations always unlit
					lit = false;
				}
				else if (m_followObject.Location == ml)
				{
					// current location always lit
					lit = true;
				}
				else if (Math.Abs(m_followObject.Location.X - ml.X) > m_followObject.VisionRange ||
					Math.Abs(m_followObject.Location.Y - ml.Y) > m_followObject.VisionRange)
				{
					// out of vision range
					lit = false;
				}
				else if (m_followObject.VisibilityMap[ml - m_followObject.Location] == false)
				{
					// can't see
					lit = false;
				}
				else
				{
					// else in range, not blocked
					lit = true;
				}
			}

			bmp = GetBitmap(ml, lit);
			tile.Bitmap = bmp;

			if(GameData.Data.DisableLOS)
				lit = true; // lit always so we see what server sends

			if (lit)
				bmp = GetObjectBitmap(ml, lit);
			else
				bmp = null;
			tile.ObjectBitmap = bmp;
		}

		BitmapSource GetBitmap(Location ml, bool lit)
		{
			int terrainID = this.Map.GetTerrainType(ml);
			return m_bitmapCache.GetBitmap(terrainID, !lit);
		}

		BitmapSource GetObjectBitmap(Location ml, bool lit)
		{
			IList<ClientGameObject> obs = this.Map.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				int id = obs[0].SymbolID;
				return m_bitmapCache.GetBitmap(id, !lit);
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

		public ClientGameObject FollowObject
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

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("FollowObject"));
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

				if (this.SelectedTile != null)
				{
					Canvas.SetLeft(m_hiliteRectangle, MapToScreen(this.SelectedTile.Location).X * m_tileSize);
					Canvas.SetTop(m_hiliteRectangle, MapToScreen(this.SelectedTile.Location).Y * m_tileSize);
				}
			}
			//MyDebug.WriteLine(String.Format("FollowedObjectMoved {0}, center {1}", l, m_center));
		}

		Location ScreenToMap(Location sl)
		{
			return sl + m_screenToMapDelta;
		}

		Location MapToScreen(Location ml)
		{
			return ml - m_screenToMapDelta;
		}

		public Location MapLocationFromPoint(Point p)
		{
			Location sl = map.LocationFromPoint(TranslatePoint(p, map));
			Location ml = ScreenToMap(sl);
			return ml;
		}

		TileInfo m_tileInfo;
		public TileInfo SelectedTile
		{
			get { return m_tileInfo; }
			set
			{
				if (m_tileInfo != null)
					m_tileInfo.StopObserve();
				m_tileInfo = value;
				m_tileInfo.StartObserve();
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("SelectedTile"));
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			this.Focus();

			if (e.LeftButton != MouseButtonState.Pressed)
			{
				base.OnMouseDown(e);
				return;
			}

			Location ml = MapLocationFromPoint(e.GetPosition(this));
			this.SelectedTile = new TileInfo(m_mapLevel, ml);

			Canvas.SetLeft(m_hiliteRectangle, MapToScreen(this.SelectedTile.Location).X * m_tileSize);
			Canvas.SetTop(m_hiliteRectangle, MapToScreen(this.SelectedTile.Location).Y * m_tileSize);

			e.Handled = true;
		}
	}

	class TileInfo : INotifyPropertyChanged
	{
		MapLevel m_mapLevel;
		Location m_location;
		public TileInfo(MapLevel mapLevel, Location location)
		{
			m_mapLevel = mapLevel;
			m_location = location;
		}

		public void StartObserve()
		{
			m_mapLevel.MapChanged += MapChanged;
		}

		public void StopObserve()
		{
			m_mapLevel.MapChanged -= MapChanged;
		}

		void MapChanged(Location l)
		{
			if (l == m_location)
			{
				Notify("TerrainType");
				Notify("Objects");
			}
		}

		public Location Location
		{
			get { return m_location; }
		}

		public int TerrainType
		{
			get { return m_mapLevel.GetTerrainType(m_location); }
		}

		public IList<ClientGameObject> Objects
		{
			get { return m_mapLevel.GetContents(m_location); }
		}

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
