using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyGame.Client
{
	class SymbolBitmapCache : IBitmapGenerator
	{
		class CacheData
		{
			public BitmapSource Bitmap;
			public BitmapSource BitmapDark;
		}

		SymbolDrawingCache m_symbolDrawingCache;

		CacheData[] m_blackBitmapList;
		Dictionary<SymbolID, Dictionary<Color, CacheData>> m_bitmapMap;

		int m_size = 8;
		int m_numDistinctBitmaps;

		public SymbolBitmapCache(SymbolDrawingCache symbolDrawingCache, int size)
		{
			if (size == 0)
				throw new Exception();

			m_symbolDrawingCache = symbolDrawingCache;
			m_size = size;

			var arr = (SymbolID[])Enum.GetValues(typeof(SymbolID));
			var max = (int)arr.Max() + 1;
			m_blackBitmapList = new CacheData[max];
			m_numDistinctBitmaps = max;

			m_bitmapMap = new Dictionary<SymbolID, Dictionary<Color, CacheData>>();
		}

		public int TileSize
		{
			get { return m_size; }

			set
			{
				if (m_size == 0)
					throw new Exception();

				if (m_size != value)
				{
					m_size = value;
					m_bitmapMap = new Dictionary<SymbolID, Dictionary<Color, CacheData>>();
					for (int i = 0; i < m_blackBitmapList.Length; ++i)
						m_blackBitmapList[i] = null;
				}
			}
		}

		public int NumDistinctBitmaps 
		{
			get { return m_numDistinctBitmaps; }
		}

		public BitmapSource GetBitmap(SymbolID symbolID, Color color, bool dark)
		{
			Dictionary<Color, CacheData> map;
			CacheData data;

			if (color == Colors.Black)
			{
				data = m_blackBitmapList[(int)symbolID];

				if (data == null)
				{
					data = new CacheData();
					m_blackBitmapList[(int)symbolID] = data;
				}
			}
			else
			{
				if (!m_bitmapMap.TryGetValue(symbolID, out map))
				{
					map = new Dictionary<Color, CacheData>();
					m_bitmapMap[symbolID] = map;
				}

				if (!map.TryGetValue(color, out data))
				{
					data = new CacheData();
					map[color] = data;
				}
			}

			if (!dark)
			{
				if (data.Bitmap == null)
				{
					BitmapSource bmp = CreateSymbolBitmap(symbolID, color, dark);
					data.Bitmap = bmp;
				}

				return data.Bitmap;
			}
			else
			{
				if (data.BitmapDark == null)
				{
					BitmapSource bmp = CreateSymbolBitmap(symbolID, color, dark);
					data.BitmapDark = bmp;
				}

				return data.BitmapDark;
			}
		}

		BitmapSource CreateSymbolBitmap(SymbolID symbolID, Color color, bool dark)
		{
			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			Drawing d = m_symbolDrawingCache.GetDrawing(symbolID, color);

			drawingContext.PushTransform(new ScaleTransform((double)m_size / 100, (double)m_size / 100));
			drawingContext.DrawDrawing(d);
			drawingContext.Pop();

			drawingContext.Close();

			if (dark)
				drawingVisual.Opacity = 0.2;

			RenderTargetBitmap bmp = new RenderTargetBitmap(m_size, m_size, 96, 96, PixelFormats.Default);
			bmp.Render(drawingVisual);
			bmp.Freeze();
			return bmp;
		}
	}
}
