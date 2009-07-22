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
	public class MapControl : UserControl
	{
		SymbolBitmapCache m_bitmapCache;

		DispatcherTimer m_updateTimer;
		Location m_screenToMapDelta;

		Location m_center;
		ClientGameObject m_followObject;
		MapLevel m_mapLevel;

		int m_tileSize;
		MapControlBase map;

		public MapControl()
		{
			m_bitmapCache = new SymbolBitmapCache();
			m_bitmapCache.SymbolDrawings = GameData.Data.SymbolDrawings.Drawings;

			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(30);

			map = new MapControlBase();
			map.DimensionsChangedEvent += MapControl_DimensionsChanged;
			this.AddChild(map);

			this.TileSize = 40;
		}

		public int TileSize
		{
			get { return m_tileSize; }

			set
			{
				m_tileSize = value;
				map.TileSize = value;
				m_bitmapCache.TileSize = value;
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
			for (int y = 0; y < map.Rows; y++)
				for (int x = 0; x < map.Columns; x++)
					UpdateTile(new Location(x, y));
		}

		void UpdateTile(Location sl)
		{
			BitmapSource bmp = null;
			MapTile tile;

			tile = map.GetTile(sl);

			bmp = GetBitmap(sl);
			tile.Bitmap = bmp;

			bmp = GetObjectBitmap(sl);
			tile.ObjectBitmap = bmp;
		}

		BitmapSource GetBitmap(Location sl)
		{
			if (this.Map == null)
				return null;

			Location ml = sl + m_screenToMapDelta;

			int terrainID = this.Map.GetTerrainType(ml);
			return m_bitmapCache.GetBitmap(terrainID, false);
		}

		BitmapSource GetObjectBitmap(Location sl)
		{
			if (this.Map == null)
				return null;

			Location ml = sl + m_screenToMapDelta;

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

	}
}
