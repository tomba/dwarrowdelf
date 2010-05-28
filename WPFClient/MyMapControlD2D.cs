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

		public MyMapControlD2D()
		{
			this.HoverTileInfo = new HoverTileInfo();
			this.SelectedTileInfo = new TileInfo();

			m_updateTimer = new DispatcherTimer(DispatcherPriority.Normal);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(100);

			this.TileSize = 32;

			m_mcd2d = new MapControlD2D();
			AddChild(m_mcd2d);
		}

		void UpdateTimerTick(object sender, EventArgs e)
		{
			m_updateTimer.Stop();
			UpdateTiles();
		}

		void Upda()
		{
			if (m_bitmapCache == null)
			{
				m_mcd2d.SetTiles(null, 0);
			}
			else
			{
				var arr = (SymbolID[])Enum.GetValues(typeof(SymbolID));
				var len = (int)arr.Max() + 1;
				m_bmpArray = new BitmapSource[len];
				for (int i = 0; i < len; ++i)
					m_bmpArray[i] = m_bitmapCache.GetBitmap((SymbolID)i, Colors.Black, false);
				m_mcd2d.SetTiles(m_bmpArray, this.TileSize);
			}
		}

		/*
		void OnTileSizeChanged(object ob, EventArgs e)
		{
			if (m_bitmapCache != null)
				m_bitmapCache.TileSize = this.TileSize;
		}
		*/
		void OnCenterPosChanged(object ob, EventArgs e)
		{
			UpdateHoverTileInfo(Mouse.GetPosition(this));
		}

		public void InvalidateTiles()
		{
			if (!m_updateTimer.IsEnabled)
				m_updateTimer.Start();
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

		public int TileSize { get; set; }
		public int Columns { get { return m_mcd2d.Columns; } }
		public int Rows { get { return m_mcd2d.Rows; } }

		public IntRect SelectionRect { get; set; }

		IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, this.Rows / 2); }
		}

		public static readonly DependencyProperty CenterPosProperty = DependencyProperty.Register(
			"CenterPos", typeof(IntPoint), typeof(MapControlD2D),
			new FrameworkPropertyMetadata(new IntPoint(), FrameworkPropertyMetadataOptions.None));

		public IntPoint CenterPos
		{
			get { return (IntPoint)GetValue(CenterPosProperty); }
			set
			{
				if (value == this.CenterPos)
					return;

				SetValue(CenterPosProperty, value);
				UpdateTiles();
			}
		}

		public bool SelectionEnabled { get; set; }

		MapHelper m_mapHelper = new MapHelper();

		void UpdateTile(int x, int y, MapD2DData[, ,] map)
		{
			//IntPoint3D ml = new IntPoint3D(_ml.X, _ml.Y, this.Z);
			var ml = new IntPoint3D(x + this.CenterPos.X, y + this.CenterPos.Y, this.Z);

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
						Upda();
					}
				}
				else
				{
					m_world = null;
					m_bitmapCache = null;
					Upda();
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

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			UpdateHoverTileInfo(e.GetPosition(this));
		}

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
