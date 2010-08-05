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
	class MapControl : MapControlBase<MapControlTile>, IMapControl, INotifyPropertyChanged
	{
		World m_world;
		SymbolBitmapCache m_bitmapCache;

		Environment m_env;
		int m_z;

		RenderView m_renderView;

		public event Action TileArrangementChanged;
		
		public MapControl()
		{
			m_renderView = new RenderView();
		}

		protected override void OnTileSizeChanged(int newSize)
		{
			if (m_bitmapCache != null)
				m_bitmapCache.TileSize = newSize;

			if (TileArrangementChanged != null)
				TileArrangementChanged();
		}

		protected override void OnGridSizeChanged(int newColumns, int newRows)
		{
			if (TileArrangementChanged != null)
				TileArrangementChanged();
		}

		protected override void OnSizeChanged()
		{
			if (TileArrangementChanged != null)
				TileArrangementChanged();
		}

		protected override void UpdateTilesOverride(MapControlTile[,] tileArray)
		{
			m_renderView.Size = new IntSize(this.Columns, this.Rows);
			m_renderView.Offset = new IntVector(this.BottomLeftPos.X, this.BottomLeftPos.Y);

			for (int y = 0; y < this.Rows; ++y)
			{
				for (int x = 0; x < this.Columns; ++x)
				{
					var tile = tileArray[y, x];

					var ml = ScreenLocationToMapLocation(new IntPoint(x, y));

					UpdateTile(tile, ml, new IntPoint(x, y));
				}
			}
		}

		void UpdateTile(MapControlTile tile, IntPoint _ml, IntPoint sl)
		{
			IntPoint3D ml = new IntPoint3D(_ml.X, _ml.Y, this.Z);

			var data = m_renderView.GetRenderTile(_ml);

			BitmapSource floorBitmap = null;
			BitmapSource interiorBitmap = null;
			BitmapSource objectBitmap = null;
			BitmapSource topBitmap = null;

			if (data.Floor.SymbolID != SymbolID.Undefined)
				floorBitmap = m_bitmapCache.GetBitmap(data.Floor.SymbolID, data.Floor.Color);

			if (data.Interior.SymbolID != SymbolID.Undefined)
				interiorBitmap = m_bitmapCache.GetBitmap(data.Interior.SymbolID, data.Interior.Color);

			if (data.Object.SymbolID != SymbolID.Undefined)
				objectBitmap = m_bitmapCache.GetBitmap(data.Object.SymbolID, data.Object.Color);

			if (data.Top.SymbolID != SymbolID.Undefined)
				topBitmap = m_bitmapCache.GetBitmap(data.Top.SymbolID, data.Top.Color);

			bool update = tile.FloorBitmap != floorBitmap || tile.InteriorBitmap != interiorBitmap || tile.ObjectBitmap != objectBitmap ||
				tile.TopBitmap != topBitmap;

			if (objectBitmap != null)
			{
				if (objectBitmap.PixelWidth != this.TileSize || objectBitmap.PixelHeight != this.TileSize)
					throw new Exception();
			}

			if (update)
			{
				tile.FloorBitmap = floorBitmap;
				tile.InteriorBitmap = interiorBitmap;
				tile.ObjectBitmap = objectBitmap;
				tile.TopBitmap = topBitmap;
				tile.InvalidateVisual();
			}
		}

		public void InvalidateDrawings()
		{
			m_bitmapCache.Invalidate();
			InvalidateTiles();
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
					}
				}
				else
				{
					m_world = null;
					m_bitmapCache = null;
				}

				InvalidateTiles();

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

				InvalidateTiles();

				Notify("Z");
			}
		}

		void MapChangedCallback(IntPoint3D l)
		{
			InvalidateTiles();
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

	class MapControlTile : UIElement
	{
		public MapControlTile()
		{
			this.IsHitTestVisible = false;
		}

		public BitmapSource FloorBitmap { get; set; }
		public BitmapSource InteriorBitmap { get; set; }
		public BitmapSource ObjectBitmap { get; set; }
		public BitmapSource TopBitmap { get; set; }

		protected override void OnRender(DrawingContext drawingContext)
		{
			if (this.FloorBitmap != null)
				drawingContext.DrawImage(this.FloorBitmap, new Rect(this.RenderSize));

			if (this.InteriorBitmap != null)
				drawingContext.DrawImage(this.InteriorBitmap, new Rect(this.RenderSize));

			if (this.ObjectBitmap != null)
				drawingContext.DrawImage(this.ObjectBitmap, new Rect(this.RenderSize));

			if (this.TopBitmap != null)
				drawingContext.DrawImage(this.TopBitmap, new Rect(this.RenderSize));
		}
	}

}
