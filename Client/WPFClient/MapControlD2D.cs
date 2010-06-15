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

		TileControlD2D m_tileControlD2D;

		DispatcherTimer m_updateTimer;

		IntPoint m_centerPos;
		int m_tileSize;

		public event Action TileArrangementChanged;

		RenderView m_renderView;

		public MapControlD2D()
		{
			m_updateTimer = new DispatcherTimer(DispatcherPriority.Normal);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(20);

			m_renderView = new RenderView();

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

			if (TileArrangementChanged != null)
				TileArrangementChanged();
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

			m_renderView.Size = new IntSize(this.Columns, this.Rows);
			m_renderView.Offset = new IntVector(this.BottomLeftPos.X, this.BottomLeftPos.Y);

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

		void UpdateTile(int x, int y, MapD2DData[, ,] map)
		{
			// x and y in screen tile coordinates
			var tlp = this.TopLeftPos;
			var ml = new IntPoint3D(x + tlp.X, -y + tlp.Y, this.Z);

			var data = m_renderView.GetRenderTile(ml.ToIntPoint());

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

		public int TileSize
		{
			get
			{
				return m_tileSize;
			}

			set
			{
				value = MyMath.IntClamp(value, 64, 8);
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

		IntPoint BottomLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, -this.Rows / 2); }
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

		public bool ShowVirtualSymbols
		{
			get { return m_renderView.ShowVirtualSymbols; }

			set
			{
				m_renderView.ShowVirtualSymbols = value;
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
					m_env.MapTileChanged -= MapChangedCallback;
				}

				m_env = value;
				m_renderView.Environment = value;

				if (m_env != null)
				{
					m_env.MapTileChanged += MapChangedCallback;

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
				m_renderView.Z = value;

				UpdateTiles();

				Notify("Z");
			}
		}

		void MapChangedCallback(IntPoint3D l)
		{
			InvalidateTiles();
		}

		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var sl = ScreenPointToScreenLocation(p);
			return ScreenLocationToMapLocation(sl);
		}

		public Point MapLocationToScreenPoint(IntPoint ml)
		{
			var sl = MapLocationToScreenLocation(ml);
			return ScreenLocationToScreenPoint(sl);
		}

		public IntPoint MapLocationToScreenLocation(IntPoint ml)
		{
			return new IntPoint(ml.X - this.TopLeftPos.X, -(ml.Y - this.TopLeftPos.Y));
		}

		public IntPoint ScreenLocationToMapLocation(IntPoint sl)
		{
			return new IntPoint(sl.X + this.TopLeftPos.X, -(sl.Y - this.TopLeftPos.Y));
		}

		public IntPoint ScreenPointToScreenLocation(Point p)
		{
			return m_tileControlD2D.ScreenPointToScreenLocation(p);
		}

		public Point ScreenLocationToScreenPoint(IntPoint loc)
		{
			return m_tileControlD2D.ScreenLocationToScreenPoint(loc);
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
