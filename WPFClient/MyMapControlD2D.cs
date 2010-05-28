using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace MyGame.Client
{
	class MyMapControlD2D : UserControl, INotifyPropertyChanged
	{
		World m_world;
		SymbolBitmapCache m_bitmapCache;

		Environment m_env;
		int m_z;

		public HoverTileInfo HoverTileInfo { get; private set; }

		bool m_showVirtualSymbols = true;

		MapControlD2D m_mcd2d;
		BitmapSource[] m_bmpArray;

		DispatcherTimer m_updateTimer;
		DispatcherTimer m_hoverTimer;

		IntPoint m_centerPos;
		int m_tileSize = 32;

		Rectangle m_selectionRect;
		IntPoint m_selectionStart;
		IntPoint m_selectionEnd;

		Canvas m_canvas;

		ToolTip m_tileToolTip;
		
		public MyMapControlD2D()
		{
			this.HoverTileInfo = new HoverTileInfo();
			this.SelectedTileInfo = new TileInfo();

			m_updateTimer = new DispatcherTimer(DispatcherPriority.Normal);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(20);

			m_hoverTimer = new DispatcherTimer(DispatcherPriority.Normal);
			m_hoverTimer.Tick += HoverTimerTick;
			m_hoverTimer.Interval = TimeSpan.FromMilliseconds(500);

			var grid = new Grid();
			AddChild(grid);

			m_mcd2d = new MapControlD2D();
			grid.Children.Add(m_mcd2d);

			m_canvas = new Canvas();
			m_canvas.ClipToBounds = true;
			grid.Children.Add(m_canvas);

			m_selectionRect = new Rectangle();
			m_selectionRect.Visibility = Visibility.Hidden;
			m_selectionRect.Width = this.TileSize;
			m_selectionRect.Height = this.TileSize;
			m_selectionRect.Stroke = Brushes.Blue;
			m_selectionRect.StrokeThickness = 1;
			m_selectionRect.Fill = new SolidColorBrush(Colors.Blue);
			m_selectionRect.Fill.Opacity = 0.2;
			m_selectionRect.Fill.Freeze();
			m_canvas.Children.Add(m_selectionRect);
		}

		public int Columns { get { return m_mcd2d.Columns; } }
		public int Rows { get { return m_mcd2d.Rows; } }

		void UpdateTileBitmaps()
		{
			if (m_bitmapCache == null)
			{
				m_mcd2d.SetSymbolBitmaps(null, 0);
			}
			else
			{
				var arr = (SymbolID[])Enum.GetValues(typeof(SymbolID));
				var len = (int)arr.Max() + 1;
				m_bmpArray = new BitmapSource[len];
				for (int i = 0; i < len; ++i)
					m_bmpArray[i] = m_bitmapCache.GetBitmap((SymbolID)i, Colors.Black, false);
				m_mcd2d.SetSymbolBitmaps(m_bmpArray, this.TileSize);
			}
		}

		public void InvalidateTiles()
		{
			if (!m_updateTimer.IsEnabled)
				m_updateTimer.Start();
		}

		void UpdateTimerTick(object sender, EventArgs e)
		{
			m_updateTimer.Stop();
			UpdateTiles();
		}

		void UpdateTiles()
		{
			int columns = this.Columns;
			int rows = this.Rows;
			var map = m_mcd2d.TileMap;

			if (map != null)
			{
				for (int y = 0; y < rows; ++y)
				{
					for (int x = 0; x < columns; ++x)
					{
						UpdateTile(x, y, map);
					}
				}
			}

			m_mcd2d.Render();
		}

		public int TileSize
		{
			get
			{
				return m_tileSize;
			}

			set
			{
				m_tileSize = value;
				if (m_bitmapCache != null)
					m_bitmapCache.TileSize = this.TileSize;
				UpdateTileBitmaps();
				UpdateTiles();
				UpdateSelectionRect();
			}
		}

		public bool SelectionEnabled { get; set; }

		void OnSelectionChanged()
		{
			IntRect sel = this.SelectionRect;

			if (sel.Width != 1 || sel.Height != 1)
			{
				this.SelectedTileInfo.Environment = null;
				return;
			}

			this.SelectedTileInfo.Environment = this.Environment;
			this.SelectedTileInfo.Location = new IntPoint3D(sel.X1Y1, this.Z);
		}

		public IntRect SelectionRect
		{
			get
			{
				if (m_selectionRect.Visibility != Visibility.Visible)
					return new IntRect();

				var p1 = m_selectionStart;
				var p2 = m_selectionEnd;

				IntRect r = new IntRect(p1, p2);
				r = r.Inflate(1, 1);
				return r;
			}

			set
			{
				if (this.SelectionEnabled == false)
					return;

				if (value.Width == 0 || value.Height == 0)
				{
					m_selectionRect.Visibility = Visibility.Hidden;
					OnSelectionChanged();
					return;
				}

				var newStart = value.X1Y1;
				var newEnd = value.X2Y2 - new IntVector(1, 1);

				if ((newStart != m_selectionStart) || (newEnd != m_selectionEnd))
				{
					m_selectionStart = newStart;
					m_selectionEnd = newEnd;
					UpdateSelectionRect();
				}

				m_selectionRect.Visibility = Visibility.Visible;

				OnSelectionChanged();
			}
		}

		void UpdateSelectionRect()
		{
			var ir = new IntRect(m_selectionStart, m_selectionEnd);
			ir = new IntRect(ir.X1Y1, new IntSize(ir.Width + 1, ir.Height + 1));

			Rect r = new Rect(MapLocationToScreenPoint(new IntPoint(ir.X1, ir.Y2)),
				new Size(ir.Width * this.TileSize, ir.Height * this.TileSize));

			m_selectionRect.Width = r.Width;
			m_selectionRect.Height = r.Height;
			Canvas.SetLeft(m_selectionRect, r.Left);
			Canvas.SetTop(m_selectionRect, r.Top);
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (this.SelectionEnabled == false)
				return;

			if (e.LeftButton != MouseButtonState.Pressed)
			{
				base.OnMouseDown(e);
				return;
			}

			Point pos = e.GetPosition(this);

			var newStart = ScreenPointToMapLocation(pos);
			var newEnd = newStart;

			if ((newStart != m_selectionStart) || (newEnd != m_selectionEnd))
			{
				m_selectionStart = newStart;
				m_selectionEnd = newEnd;
				UpdateSelectionRect();
			}

			m_selectionRect.Visibility = Visibility.Visible;

			CaptureMouse();

			e.Handled = true;

			OnSelectionChanged();

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			UpdateHoverTileInfo(e.GetPosition(this));

			m_hoverTimer.Stop();
			m_hoverTimer.Start();

			if (this.SelectionEnabled == false)
				return;

			if (!IsMouseCaptured)
			{
				base.OnMouseMove(e);
				return;
			}

			Point pos = e.GetPosition(this);

			int limit = 4;
			int cx = this.CenterPos.X;
			int cy = this.CenterPos.Y;

			if (this.ActualWidth - pos.X < limit)
				++cx;
			else if (pos.X < limit)
				--cx;

			if (this.ActualHeight - pos.Y < limit)
				--cy;
			else if (pos.Y < limit)
				++cy;

			var p = new IntPoint(cx, cy);
			this.CenterPos = p;

			var newEnd = ScreenPointToMapLocation(pos);

			if (newEnd != m_selectionEnd)
			{
				m_selectionEnd = newEnd;
				UpdateSelectionRect();
			}

			e.Handled = true;

			OnSelectionChanged();

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (this.SelectionEnabled == false)
				return;

			ReleaseMouseCapture();

			base.OnMouseUp(e);
		}

		void HoverTimerTick(object sender, EventArgs e)
		{
			MyDebug.WriteLine("hover");

			if (this.Environment == null)
				return;

			if (m_tileToolTip == null)
			{
				m_tileToolTip = new ToolTip();
				m_tileToolTip.Content = new ObjectInfoControl();
				this.ToolTip = m_tileToolTip;
				m_tileToolTip.PlacementTarget = this;
				m_tileToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint;
			}

			var p = Mouse.GetPosition(this);

			var ml = new IntPoint3D(ScreenPointToMapLocation(p), this.Z);
			var objectList = this.Environment.GetContents(ml);
			if (objectList == null || objectList.Count == 0)
			{
				m_tileToolTip.IsOpen = false;
				return;
			}

			m_tileToolTip.DataContext = objectList[0];

			m_tileToolTip.HorizontalOffset = p.X;
			m_tileToolTip.VerticalOffset = p.Y;
			m_tileToolTip.IsOpen = true;
		}


		IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, this.Rows / 2); }
		}

		public IntPoint CenterPos
		{
			get { return m_centerPos; }
			set
			{
				if (value == this.CenterPos)
					return;
				m_centerPos = value;
				UpdateHoverTileInfo(Mouse.GetPosition(this));
				UpdateSelectionRect();
				UpdateTiles();
			}
		}

		MapHelper m_mapHelper = new MapHelper();

		void UpdateTile(int x, int y, MapD2DData[, ,] map)
		{
			// x and y in screen tile coordinates
			var tlp = this.TopLeftPos;
			var ml = new IntPoint3D(x + tlp.X, -y + tlp.Y, this.Z);

			var data = m_mapHelper;

			data.Resolve(this.Environment, ml, m_showVirtualSymbols);

			map[y, x, 0].SymbolID = (byte)data.FloorSymbolID;
			map[y, x, 1].SymbolID = (byte)data.InteriorSymbolID;
			map[y, x, 2].SymbolID = (byte)data.ObjectSymbolID;
			map[y, x, 3].SymbolID = (byte)data.TopSymbolID;

			map[y, x, 0].Dark = data.FloorDark;
			map[y, x, 1].Dark = data.InteriorDark;
			map[y, x, 2].Dark = data.ObjectDark;
			map[y, x, 3].Dark = data.TopDark;
		}

		public bool ShowVirtualSymbols
		{
			get { return m_showVirtualSymbols; }

			set
			{
				if (m_showVirtualSymbols == value)
					return;

				m_showVirtualSymbols = value;
				InvalidateTiles();
				Notify("ShowVirtualSymbols");
			}
		}

		public Environment Environment
		{
			get { return m_env; }

			set
			{
				if (m_env == value)
					return;

				if (m_env != null)
				{
					m_env.MapChanged -= MapChangedCallback;
					m_env.Buildings.CollectionChanged -= OnBuildingsChanged;
				}

				m_env = value;

				if (m_env != null)
				{
					m_env.MapChanged += MapChangedCallback;
					m_env.Buildings.CollectionChanged += OnBuildingsChanged;

					if (m_world != m_env.World)
					{
						m_world = m_env.World;
						m_bitmapCache = new SymbolBitmapCache(m_world.SymbolDrawingCache, this.TileSize);
						UpdateTileBitmaps();
					}
				}
				else
				{
					m_world = null;
					m_bitmapCache = null;
					UpdateTileBitmaps();
				}

				this.SelectionRect = new IntRect();
				UpdateTiles();
				UpdateBuildings();

				Notify("Environment");
			}
		}

		void UpdateBuildings()
		{
			/*
			this.Children.Clear();

			if (m_env != null)
			{
				foreach (var b in m_env.Buildings)
				{
					if (b.Environment == m_env && b.Z == m_z)
					{
						var rect = new Rectangle();
						rect.Stroke = Brushes.DarkGray;
						rect.StrokeThickness = 4;
						this.Children.Add(rect);
						SetCorner1(rect, b.Area.X1Y1);
						SetCorner2(rect, b.Area.X2Y2);
					}
				}
			}
			*/
		}

		void OnBuildingsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			/*
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (BuildingObject b in e.NewItems)
				{
					var rect = new Rectangle();
					rect.Stroke = Brushes.DarkGray;
					rect.StrokeThickness = 4;
					this.Children.Add(rect);
					SetCorner1(rect, b.Area.X1Y1);
					SetCorner2(rect, b.Area.X2Y2);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				this.Children.Clear();
			}
			else
			{
				throw new Exception();
			}
			 */
		}

		public int Z
		{
			get { return m_z; }

			set
			{
				if (m_z == value)
					return;

				m_z = value;
				UpdateTiles();
				UpdateBuildings();

				Notify("Z");
				UpdateHoverTileInfo(Mouse.GetPosition(this));
			}
		}

		void MapChangedCallback(IntPoint3D l)
		{
			InvalidateTiles();
		}

		public TileInfo SelectedTileInfo { get; private set; }

		void UpdateHoverTileInfo(Point mousePos)
		{
			IntPoint ml = ScreenPointToMapLocation(mousePos);
			var p = new IntPoint3D(ml, m_z);

			if (p != this.HoverTileInfo.Location)
			{
				this.HoverTileInfo.Location = p;
				Notify("HoverTileInfo");
			}
		}

		IntPoint ScreenPointToScreenLocation(Point p)
		{
			return new IntPoint((int)(p.X / this.TileSize), (int)(p.Y / this.TileSize));
		}

		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var loc = ScreenPointToScreenLocation(p);
			loc = new IntPoint(loc.X, -loc.Y);
			return loc + (IntVector)this.TopLeftPos;
		}

		Point MapLocationToScreenPoint(IntPoint loc)
		{
			loc -= (IntVector)this.TopLeftPos;
			loc = new IntPoint(loc.X, -loc.Y + 1);
			return new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
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
