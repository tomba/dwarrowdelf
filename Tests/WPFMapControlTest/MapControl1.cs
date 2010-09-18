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
	class MapControl1 : MapControlBase
	{
		Map m_map;
		DrawingCache m_drawingCache;
		SymbolDrawingCache m_symbolDrawingCache;
		SymbolBitmapCache m_symbolBitmapCache;

		public MapControl1()
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

		protected override UIElement CreateTile()
		{
			return new MapControlTile();
		}

		// called for each visible tile
		protected override void UpdateTile(UIElement _tile, IntPoint ml)
		{
			MapControlTile tile = (MapControlTile)_tile;

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

		class MapControlTile : UIElement
		{
			public MapControlTile()
			{
				this.IsHitTestVisible = false;
			}

			public static readonly DependencyProperty BitmapProperty = DependencyProperty.Register(
				"Bitmap", typeof(BitmapSource), typeof(MapControlTile),
				new PropertyMetadata(ValueChangedCallback));

			public BitmapSource Bitmap
			{
				get { return (BitmapSource)GetValue(BitmapProperty); }
				set { SetValue(BitmapProperty, value); }
			}

			static void ValueChangedCallback(DependencyObject ob, DependencyPropertyChangedEventArgs e)
			{
				((MapControlTile)ob).InvalidateVisual();
			}

			protected override void OnRender(DrawingContext drawingContext)
			{
				drawingContext.DrawImage(this.Bitmap, new Rect(this.RenderSize));
			}
		}
	}
}
