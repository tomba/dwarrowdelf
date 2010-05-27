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

using MyGame;
using MyGame.Client;

namespace WPFMapControlTest
{
	class MapControl4 : UserControl
	{
		Map m_map;
		int m_tileSize = 16;

		DrawingCache m_drawingCache;
		SymbolDrawingCache m_symbolDrawingCache;
		SymbolBitmapCache m_symbolBitmapCache;

		public MapControl4()
		{
			m_drawingCache = new DrawingCache();
			m_symbolDrawingCache = new SymbolDrawingCache(m_drawingCache);
			m_symbolBitmapCache = new SymbolBitmapCache(m_symbolDrawingCache, m_tileSize);

			m_map = new Map(512, 512);
			for (int y = 0; y < m_map.Height; ++y)
			{
				for (int x = 0; x < m_map.Width; ++x)
				{
					m_map.MapArray[y, x] = (x + (y % 2)) % 2 == 0 ? (byte)50 : (byte)255;
				}
			}

			BitmapSource[] bmpArr = new BitmapSource[10];
			for (int i = 0; i < 10; ++i)
				bmpArr[i] = m_symbolBitmapCache.GetBitmap((SymbolID)i, Colors.Black, false);

			var mcd2d = new MapControlD2D();
			mcd2d.TileSize = m_tileSize;
			mcd2d.BitmapArray = bmpArr;
			this.AddChild(mcd2d);
		}

		public double TileSize { get; set; }
		public IntPoint CenterPos { get; set; }

	}
}
