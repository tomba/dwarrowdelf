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
	/// <summary>
	/// Wraps low level tilemap. Handles Environment, position.
	/// </summary>
	class MapControlD2D : UserControl, IMapControl, INotifyPropertyChanged
	{
		World m_world;
		SymbolBitmapCache m_bitmapCache;

		Environment m_env;
		int m_z;

		bool m_showVirtualSymbols = true;

		TileControlD2D m_tileControlD2D;

		DispatcherTimer m_updateTimer;

		IntPoint m_centerPos;
		int m_tileSize;

		public event Action MapChanged;

		public MapControlD2D()
		{
			m_updateTimer = new DispatcherTimer(DispatcherPriority.Normal);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(20);

			m_tileControlD2D = new TileControlD2D();
			m_tileControlD2D.TileMapChanged += OnTileMapChanged;
			AddChild(m_tileControlD2D);
		}

		public int Columns { get { return m_tileControlD2D.Columns; } }
		public int Rows { get { return m_tileControlD2D.Rows; } }

		// Called when underlying TileControl changes
		void OnTileMapChanged()
		{
			UpdateTiles();

			if (MapChanged != null)
				MapChanged();
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
			MyDebug.WriteLine("Update TileMap");

			int columns = this.Columns;
			int rows = this.Rows;
			var map = m_tileControlD2D.TileMap;

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

			m_tileControlD2D.Render();
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
					m_bitmapCache.TileSize = value;
				m_tileControlD2D.TileSize = value;
				UpdateTiles();
			}
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

			map[y, x, 0].Color = GameColor.None;
			map[y, x, 1].Color = GameColor.None;
			map[y, x, 2].Color = data.ObjectColor;
			map[y, x, 3].Color = GameColor.None;
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
				}

				m_env = value;

				if (m_env != null)
				{
					m_env.MapChanged += MapChangedCallback;

					if (m_world != m_env.World)
					{
						m_world = m_env.World;
						m_bitmapCache = new SymbolBitmapCache(m_world.SymbolDrawingCache, this.TileSize);
						m_tileControlD2D.BitmapGenerator = m_bitmapCache;
					}
				}
				else
				{
					m_world = null;
					m_bitmapCache = null;
					m_tileControlD2D.BitmapGenerator = null;
				}

				UpdateTiles();

				Notify("Environment");
			}
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

				Notify("Z");
			}
		}

		void MapChangedCallback(IntPoint3D l)
		{
			InvalidateTiles();
		}

		public IntPoint ScreenPointToScreenLocation(Point p)
		{
			return new IntPoint((int)(p.X / this.TileSize), (int)(p.Y / this.TileSize));
		}

		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var loc = ScreenPointToScreenLocation(p);
			loc = new IntPoint(loc.X, -loc.Y);
			return loc + (IntVector)this.TopLeftPos;
		}

		public Point MapLocationToScreenPoint(IntPoint loc)
		{
			loc -= (IntVector)this.TopLeftPos;
			loc = new IntPoint(loc.X, -loc.Y + 1);
			return new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
		}

		public Point ScreenLocationToScreenPoint(IntPoint loc)
		{
			throw new NotImplementedException();
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
