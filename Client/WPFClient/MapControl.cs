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
	class MapControl : MapControlBase, IMapControl, INotifyPropertyChanged
	{
		World m_world;
		SymbolBitmapCache m_bitmapCache;

		Environment m_env;
		int m_z;

		bool m_showVirtualSymbols = true;

		RenderView m_renderView;

		public MapControl()
		{
			m_renderView = new RenderView();

			var dpd = DependencyPropertyDescriptor.FromProperty(MapControlBase.TileSizeProperty,
				typeof(MapControlBase));
			dpd.AddValueChanged(this, OnTileSizeChanged);
		}

		void OnTileSizeChanged(object ob, EventArgs e)
		{
			if (m_bitmapCache != null)
				m_bitmapCache.TileSize = this.TileSize;
		}

		protected override UIElement CreateTile()
		{
			return new MapControlTile();
		}

		protected override void UpdateTile(UIElement _tile, IntPoint _ml, IntPoint sl)
		{
			var size = new IntSize(this.Columns, this.Rows);
			if (m_renderView.Size != size)
				m_renderView.Size = size;
			var v = new IntVector(-this.TopLeftPos.X, this.Rows - this.TopLeftPos.Y - 1);
			if (v != m_renderView.Offset)
				m_renderView.Offset = v;

			var tile = (MapControlTile)_tile;
			IntPoint3D ml = new IntPoint3D(_ml.X, _ml.Y, this.Z);

			var data = m_renderView.GetRenderTile(_ml);

			BitmapSource floorBitmap = null;
			BitmapSource interiorBitmap = null;
			BitmapSource objectBitmap = null;
			BitmapSource topBitmap = null;

			if (data.FloorSymbolID != SymbolID.Undefined)
				floorBitmap = m_bitmapCache.GetBitmap(data.FloorSymbolID, GameColor.None, data.FloorDark);

			if (data.InteriorSymbolID != SymbolID.Undefined)
				interiorBitmap = m_bitmapCache.GetBitmap(data.InteriorSymbolID, GameColor.None, data.InteriorDark);

			if (data.ObjectSymbolID != SymbolID.Undefined)
				objectBitmap = m_bitmapCache.GetBitmap(data.ObjectSymbolID, data.ObjectColor, data.ObjectDark);

			if (data.TopSymbolID != SymbolID.Undefined)
				topBitmap = m_bitmapCache.GetBitmap(data.TopSymbolID, GameColor.None, data.TopDark);

			bool update = tile.FloorBitmap != floorBitmap || tile.InteriorBitmap != interiorBitmap || tile.ObjectBitmap != objectBitmap ||
				tile.TopBitmap != topBitmap;

			if (update)
			{
				tile.FloorBitmap = floorBitmap;
				tile.InteriorBitmap = interiorBitmap;
				tile.ObjectBitmap = objectBitmap;
				tile.TopBitmap = topBitmap;
				tile.InvalidateVisual();
			}
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

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

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

}
