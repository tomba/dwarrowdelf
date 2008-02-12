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
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace MyGame
{
	class MapControl : FrameworkElement
	{
		SymbolDrawings m_symbolDrawings;
		BitmapSource[] m_symbolBitmaps;
		BitmapSource[] m_symbolBitmapsDark;

		LocationGrid<MapTile> m_mapTiles;

		int m_columns = 5;
		int m_rows = 5;
		
		Location m_center;
		ClientGameObject m_followObject;

		double m_tileSize = 40;

		MapLevel m_mapLevel;

		Canvas m_effectsCanvas = new Canvas();
		Rectangle m_hiliteRectangle = new Rectangle();

		DispatcherTimer m_updateTimer;

		public MapControl()
		{
			this.Focusable = true;

			m_symbolDrawings = new SymbolDrawings();
			m_symbolBitmaps = new BitmapSource[m_symbolDrawings.Count];
			m_symbolBitmapsDark = new BitmapSource[m_symbolDrawings.Count];
			CreateSymbolBitmaps();

			CreateMapTiles();

			this.AddVisualChild(m_effectsCanvas);

			m_effectsCanvas.Children.Add(m_hiliteRectangle);
			m_hiliteRectangle.Width = m_tileSize;
			m_hiliteRectangle.Height = m_tileSize;
			m_hiliteRectangle.Stroke = Brushes.Blue;
			m_hiliteRectangle.StrokeThickness = 2;

			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(10);
		}

		void CreateMapTiles()
		{
			m_mapTiles = new LocationGrid<MapTile>(m_columns, m_rows);

			foreach(Location l in m_mapTiles.GetLocations())
			{
				MapTile tile = new MapTile(this);
				m_mapTiles[l] = tile;
				this.AddVisualChild(tile);
			}
		}

		void RemoveMapTiles()
		{
			if (m_mapTiles == null)
				return;

			foreach (Location l in m_mapTiles.GetLocations())
			{
				this.RemoveVisualChild(m_mapTiles[l]);
				m_mapTiles[l] = null;
			}

			m_mapTiles = null;
		}

		public MapLevel MapLevel
		{
			get
			{
				return m_mapLevel;
			}

			set
			{
				if (m_mapLevel != null)
					m_mapLevel.MapChanged -= MapChangedCallback;
				m_mapLevel = value;
				m_mapLevel.MapChanged += MapChangedCallback;
				InvalidateVisual();
			}
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
			}

		}

		public double TileSize
		{
			get
			{
				return m_tileSize;
			}

			set
			{
				if (value < 2)
					value = 2;

				if (m_tileSize != value)
				{
					m_tileSize = value;

					CreateSymbolBitmaps();

					m_hiliteRectangle.Width = m_tileSize;
					m_hiliteRectangle.Height = m_tileSize;

					InvalidateVisual();
				}
			}
		}

		void CreateSymbolBitmaps()
		{
			MyDebug.WriteLine("CreateSymbolBitmaps");

			for (int i = 0; i < m_symbolBitmaps.Length; i++)
			{
				DrawingVisual drawingVisual = new DrawingVisual();
				DrawingContext drawingContext = drawingVisual.RenderOpen();

				Drawing d = m_symbolDrawings[i];

				drawingContext.PushTransform(
					new ScaleTransform(Math.Floor(m_tileSize) / d.Bounds.Width, Math.Floor(m_tileSize) / d.Bounds.Height));
				drawingContext.PushTransform(
					new TranslateTransform(-d.Bounds.X, -d.Bounds.Y));
				
				drawingContext.DrawDrawing(d);
				drawingContext.Pop();
				drawingContext.Pop();

				drawingContext.Close();

				RenderTargetBitmap bmp = new RenderTargetBitmap((int)m_tileSize, (int)m_tileSize, 96, 96, PixelFormats.Default);
				bmp.Render(drawingVisual);
				bmp.Freeze();
				m_symbolBitmaps[i] = bmp;

				drawingVisual.Opacity = 0.2;

				bmp = new RenderTargetBitmap((int)m_tileSize, (int)m_tileSize, 96, 96, PixelFormats.Default);
				bmp.Render(drawingVisual);
				bmp.Freeze();
				m_symbolBitmapsDark[i] = bmp;
			}
		}

		protected override Size MeasureOverride(Size s)
		{
			//MyDebug.WriteLine(String.Format("MeasureOverride {0}", s));

			int columns;
			int rows;

			if (Double.IsInfinity(s.Width))
				columns = 20;
			else
				columns = (int)(s.Width / m_tileSize);

			if (Double.IsInfinity(s.Height))
				rows = 20;
			else
				rows = (int)(s.Height / m_tileSize);

			m_effectsCanvas.Measure(s);

			return new Size(columns * m_tileSize, rows * m_tileSize);
		}

		protected override Size ArrangeOverride(Size s)
		{
			//MyDebug.WriteLine(String.Format("ArrangeOverride {0}", s));

			int newColumns = (int)(s.Width / m_tileSize);
			int newRows = (int)(s.Height / m_tileSize);

			if (newColumns != m_columns || newRows != m_rows)
			{
				RemoveMapTiles();

				m_columns = newColumns;
				m_rows = newRows;

				MyDebug.WriteLine(String.Format("new cols {0}, new rows {1}", m_columns, m_rows));

				CreateMapTiles();

				if (m_mapLevel != null)
				{
					if (!m_updateTimer.IsEnabled)
						m_updateTimer.Start();

					//PopulateMapTiles();
				}
			}

			foreach(Location l in m_mapTiles.GetLocations())
			{
					m_mapTiles[l].Arrange(new Rect(l.X * m_tileSize, l.Y * m_tileSize, m_tileSize, m_tileSize));
			}

			m_effectsCanvas.Arrange(new Rect(this.RenderSize));

			return base.ArrangeOverride(s);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(Brushes.Black, null, new Rect(this.RenderSize));
		}

		protected override int VisualChildrenCount
		{
			get
			{
				return m_mapTiles.Width * m_mapTiles.Height + 1; // +1 for effect canvas
			}
		}

		protected override Visual GetVisualChild(int index)
		{
			// canvas is last, so it's on top of tiles
			if (index == m_columns * m_rows)
				return m_effectsCanvas;

			int y = index / m_columns;
			int x = index % m_columns;
			return m_mapTiles[x, y];
		}



		void MapChangedCallback(Location ml)
		{
			//MyDebug.WriteLine(String.Format("Mapchanged {0}", ml));

			if (!m_updateTimer.IsEnabled)
				m_updateTimer.Start();
		}

		void UpdateTimerTick(object sender, EventArgs e)
		{
			MyDebug.WriteLine("UpdateTimerTick");

			m_updateTimer.Stop();
			/*
			int dx = m_center.X - m_columns / 2;
			int dy = m_center.Y - m_rows / 2;
			
			Location sl = new Location(ml.X - dx, ml.Y - dy);

			if (sl.X < 0 || sl.Y < 0 || sl.X >= m_columns || sl.Y >= m_rows)
				return;

			UpdateTile(ml, sl);
			 */

			if(m_mapLevel != null)
				PopulateMapTiles(); // xxx update all for now. this may be ok anyway, LOS etc changes quite a lot of the screen
		}



		public Location ScreenToMap(Location sl)
		{
			int dx = m_center.X - m_columns / 2;
			int dy = m_center.Y - m_rows / 2;
			return new Location(sl.X + dx, sl.Y + dy);
		}

		public Location MapToScreen(Location ml)
		{
			int dx = m_center.X - m_columns / 2;
			int dy = m_center.Y - m_rows / 2;
			return new Location(ml.X - dx, ml.Y - dy);
		}

		void PopulateMapTiles()
		{
			LocationGrid<TerrainData> terrainData = m_mapLevel.GetTerrain();
			
			LOSShadowCast1.CalculateLOS(m_followObject.Location, m_followObject.VisionRange,
				m_followObject.VisibilityMap, m_mapLevel.Bounds, 
				(Location l) => { return terrainData[l].m_terrainID == 2; });
			
			int dx = m_center.X - m_columns / 2;
			int dy = m_center.Y - m_rows / 2;

			for (int sy = 0; sy < m_rows; sy++)
			{
				for (int sx = 0; sx < m_columns; sx++)
				{
					int mx = sx + dx;
					int my = sy + dy;

					UpdateTile(new Location(mx, my), new Location(sx, sy));
				}
			}
		}

		void UpdateTile(Location ml, Location sl)
		{
			if (ml.X < 0 || ml.Y < 0 || ml.X >= m_mapLevel.Width || ml.Y >= m_mapLevel.Height)
			{
				m_mapTiles[sl].Bitmap = null;
				m_mapTiles[sl].ObjectBitmap = null;
				return;
			}

			int terrainID = m_mapLevel.GetTerrain(ml);
			BitmapSource bmp;
			bool lit = true;
			if (m_followObject.Location == ml)
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
				lit = true;

			if (lit)
				bmp = m_symbolBitmaps[terrainID];
			else
				bmp = m_symbolBitmapsDark[terrainID];

			m_mapTiles[sl].Bitmap = bmp;

			if(GameData.Data.DisableLOS)
				lit = true; // lit always so we see what server sends

			if (lit)
			{
				List<ClientGameObject> obs = m_mapLevel.GetContents(ml);
				if (obs != null && obs.Count > 0)
				{
					bmp = m_symbolBitmaps[obs[0].SymbolID];
					m_mapTiles[sl].ObjectBitmap = bmp;
				}
				else
					m_mapTiles[sl].ObjectBitmap = null;
			}
			else
				m_mapTiles[sl].ObjectBitmap = null;

		}

		Location TileFromPoint(Point p)
		{
			return new Location((int)(p.X / m_tileSize), (int)(p.Y / m_tileSize));
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			//MyDebug.WriteLine("GotFocus");
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			//MyDebug.WriteLine("LostFocus");
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			this.Focus();
			//MyDebug.WriteLine("Mouse down");

			if (e.RightButton == MouseButtonState.Pressed)
			{
				Location sl = TileFromPoint(e.GetPosition(this));
				Location ml = ScreenToMap(sl);

				if (!m_mapLevel.Bounds.Contains(ml))
					return;

				GameData.Data.Connection.Server.ToggleTile(ml);
			}
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			//MyDebug.WriteLine(String.Format("OnMouseWheel {0}", e.Delta));
			if (e.Delta < 0)
				this.TileSize = this.TileSize - 5;
			else
				this.TileSize = this.TileSize + 5;

			base.OnMouseWheel(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			//MyDebug.WriteLine("OnMyKeyDown");

			Direction dir;
			switch (e.Key)
			{
				case Key.Up: dir = Direction.Up; break;
				case Key.Down: dir = Direction.Down; break;
				case Key.Left: dir = Direction.Left; break;
				case Key.Right: dir = Direction.Right; break;
				case Key.Home: dir = Direction.UpLeft; break;
				case Key.End: dir = Direction.DownLeft; break;
				case Key.PageUp: dir = Direction.UpRight; break;
				case Key.PageDown: dir = Direction.DownRight; break;

				case Key.Space:
					{
						e.Handled = true;
						int wtid = GameData.Data.Connection.GetTransactionID();
						GameData.Data.Connection.DoAction(new WaitAction(wtid, GameData.Data.Player, 1));
						return;
					}

				default:
					return;
			}

			e.Handled = true;
			int tid = GameData.Data.Connection.GetTransactionID();
			GameData.Data.Connection.DoAction(new MoveAction(tid, GameData.Data.Player, dir));
		}

		void FollowedObjectMoved(MapLevel e, Location l)
		{
			if (e != m_mapLevel)
			{
				if (m_mapLevel != null)
					m_mapLevel.MapChanged -= MapChangedCallback;
				m_mapLevel = e;
				m_mapLevel.MapChanged += MapChangedCallback;

				m_center = new Location(-1, -1);
			}

			int xd = m_columns / 2;
			int yd = m_rows / 2;
			Location newCenter = new Location(((l.X+xd/2) / xd) * xd, ((l.Y+yd/2) / yd) * yd);

			if (m_center != newCenter)
			{
				m_center = newCenter;

				if (!m_updateTimer.IsEnabled)
					m_updateTimer.Start();

				//PopulateMapTiles();
			}

			Canvas.SetLeft(m_hiliteRectangle, MapToScreen(l).X * m_tileSize);
			Canvas.SetTop(m_hiliteRectangle, MapToScreen(l).Y * m_tileSize);

			//MyDebug.WriteLine(String.Format("FollowedObjectMoved {0}, center {1}", l, m_center));
		}
	}
}
