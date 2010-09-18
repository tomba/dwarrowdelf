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

using Dwarrowdelf;
using Dwarrowdelf.Client;

namespace WPFMapControlTest
{
	class MapControl2 : MapControlBase2
	{
		Map m_map;
		DrawingCache m_drawingCache;
		SymbolDrawingCache m_symbolDrawingCache;
		SymbolBitmapCache m_symbolBitmapCache;

		public MapControl2()
		{
			m_drawingCache = new DrawingCache();
			m_symbolDrawingCache = new SymbolDrawingCache(m_drawingCache);
			m_symbolBitmapCache = new SymbolBitmapCache(m_symbolDrawingCache, this.TileSize);

			m_map = new Map(512, 512);
			for (int y = 0; y < m_map.Height; ++y)
			{
				for (int x = 0; x < m_map.Width; ++x)
				{
					m_map.MapArray[y, x] = (x + (y % 2)) % 2 == 0 ? (byte)50 : (byte)255;
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			var ml = base.ScreenPointToMapLocation(e.GetPosition(this));
			if (m_map.Bounds.Contains(ml))
			{
				m_map.MapArray[ml.Y, ml.X] = (byte)(~m_map.MapArray[ml.Y, ml.X]);
				base.InvalidateTiles();
			}
		}

		protected override Visual CreateTile(double x, double y)
		{
			return new MapControlTile2(this, x, y);
		}

		// called for each visible tile
		protected override void UpdateTile(Visual _tile, IntPoint ml)
		{
			MapControlTile2 tile = (MapControlTile2)_tile;
			BitmapSource bmp;

			if (m_map.Bounds.Contains(ml))
			{
				byte b = m_map.MapArray[ml.Y, ml.X];

				if (b < 100)
					bmp = m_symbolBitmapCache.GetBitmap(SymbolID.Wall, Colors.Black, false);
				else
					bmp = m_symbolBitmapCache.GetBitmap(SymbolID.Floor, Colors.Black, false);
			}
			else
			{
				bmp = null;
			}

			if (bmp != tile.Bitmap)
			{
				tile.Bitmap = bmp;
				tile.Update();
			}
		}

		class MapControlTile2 : DrawingVisual
		{
			MapControl2 m_parent;
			public BitmapSource Bitmap { get; set; }

			public MapControlTile2(MapControl2 parent, double x, double y)
			{
				m_parent = parent;
				this.VisualOffset = new Vector(x, y);
			}

			public void Update()
			{
				var drawingContext = this.RenderOpen();
				drawingContext.DrawImage(this.Bitmap, new Rect(new Size(m_parent.TileSize, m_parent.TileSize)));
				drawingContext.Close();
			}
		}
	}

}
