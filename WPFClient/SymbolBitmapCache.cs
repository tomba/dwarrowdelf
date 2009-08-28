using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyGame
{
	class SymbolBitmapCache
	{
		class CacheData
		{
			public BitmapSource Bitmap;
			public BitmapSource BitmapDark;
		}

		SymbolDrawingCache m_symbolDrawings;

		Dictionary<int, Dictionary<Color, CacheData>> m_bitmapMap =
			new Dictionary<int,Dictionary<Color,CacheData>>();

		double m_size = 32;

		public double TileSize
		{
			get { return m_size; }

			set
			{
				m_size = value;
				m_bitmapMap = new Dictionary<int, Dictionary<Color, CacheData>>();
			}
		}

		public SymbolDrawingCache SymbolDrawings
		{
			get { return m_symbolDrawings; }

			set
			{
				m_symbolDrawings = value;
				m_bitmapMap = new Dictionary<int, Dictionary<Color, CacheData>>();
			}
		}

		public BitmapSource GetBitmap(int symbolID, Color color, bool dark)
		{
			Dictionary<Color, CacheData> map;
			CacheData data;

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

		BitmapSource CreateSymbolBitmap(int symbolID, Color color, bool dark)
		{
			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			Drawing d = m_symbolDrawings.GetDrawing(symbolID, color);

			drawingContext.PushTransform(
				new ScaleTransform(Math.Floor(m_size) / 100, Math.Floor(m_size) / 100));

			drawingContext.DrawDrawing(d);
			drawingContext.Pop();

			drawingContext.Close();

			if (dark)
				drawingVisual.Opacity = 0.2;

			RenderTargetBitmap bmp = new RenderTargetBitmap((int)m_size, (int)m_size, 96, 96, PixelFormats.Default);
			bmp.Render(drawingVisual);
			bmp.Freeze();
			return bmp;
		}
	}
}
