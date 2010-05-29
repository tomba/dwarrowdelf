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

		DrawingCache m_drawingCache;
		SymbolDrawingCache m_symbolDrawingCache;
		SymbolBitmapCache m_symbolBitmapCache;

		TileControlD2D m_mcd2d;
		int m_tileSize = 16;

		BitmapSource[] m_bmpArray;

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

			m_bmpArray = new BitmapSource[10];
			for (int i = 0; i < 10; ++i)
				m_bmpArray[i] = m_symbolBitmapCache.GetBitmap((SymbolID)i, Colors.Black, false);


			m_mcd2d = new TileControlD2D();
			m_mcd2d.SetSymbolBitmaps(m_bmpArray, m_tileSize);
			AddChild(m_mcd2d);
		}

		public double TileSize
		{
			get { return m_tileSize; }
			set
			{
				m_tileSize = (int)value;

				m_symbolBitmapCache.TileSize = value;

				for (int i = 0; i < 10; ++i)
					m_bmpArray[i] = m_symbolBitmapCache.GetBitmap((SymbolID)i, Colors.Black, false);

				m_mcd2d.SetSymbolBitmaps(m_bmpArray, m_tileSize);
			}
		}

		public IntPoint CenterPos { get; set; }

	}
}
