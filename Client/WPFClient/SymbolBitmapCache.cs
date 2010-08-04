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

		CacheData[] m_nonColoredBitmapList;
		Dictionary<GameColor, CacheData>[] m_coloredBitmapMap;

		int m_size;
		int m_numDistinctBitmaps;

		public SymbolBitmapCache(SymbolDrawingCache symbolDrawingCache, int size)
		{
			if (size == 0)
				throw new Exception();

			m_symbolDrawingCache = symbolDrawingCache;
			m_size = size;

			var arr = (SymbolID[])Enum.GetValues(typeof(SymbolID));
			var max = (int)arr.Max() + 1;
			m_numDistinctBitmaps = max;

			m_nonColoredBitmapList = new CacheData[m_numDistinctBitmaps];
			m_coloredBitmapMap = new Dictionary<GameColor, CacheData>[m_numDistinctBitmaps];
		}

		public void Invalidate()
		{
			m_nonColoredBitmapList = new CacheData[m_numDistinctBitmaps];
			m_coloredBitmapMap = new Dictionary<GameColor, CacheData>[m_numDistinctBitmaps];
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

					Array.Clear(m_nonColoredBitmapList, 0, m_nonColoredBitmapList.Length);
					Array.Clear(m_coloredBitmapMap, 0, m_nonColoredBitmapList.Length);
				}
			}
		}

		public int NumDistinctBitmaps
		{
			get { return m_numDistinctBitmaps; }
		}

		public BitmapSource GetBitmap(SymbolID symbolID, GameColor color, bool dark)
		{
			CacheData data;

			if (color == GameColor.None)
			{
				data = m_nonColoredBitmapList[(int)symbolID];

				if (data == null)
				{
					data = new CacheData();
					m_nonColoredBitmapList[(int)symbolID] = data;
				}
			}
			else
			{
				var map = m_coloredBitmapMap[(int)symbolID];

				if (map == null)
				{
					map = new Dictionary<GameColor, CacheData>();
					m_coloredBitmapMap[(int)symbolID] = map;
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

		BitmapSource CreateSymbolBitmap(SymbolID symbolID, GameColor color, bool dark)
		{
			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			var d = m_symbolDrawingCache.GetDrawing(symbolID, color);
			if (dark)
			{
				d = d.Clone();
				DarkenDrawing(d);
			}

			drawingContext.PushTransform(new ScaleTransform((double)m_size / 100, (double)m_size / 100));
			drawingContext.DrawDrawing(d);
			drawingContext.Pop();
			drawingContext.Close();

			RenderTargetBitmap bmp = new RenderTargetBitmap(m_size, m_size, 96, 96, PixelFormats.Default);
			bmp.Render(drawingVisual);
			bmp.Freeze();

			return bmp;
		}

		static void DarkenDrawing(Drawing drawing)
		{
			if (drawing is DrawingGroup)
			{
				var dg = (DrawingGroup)drawing;
				foreach (var d in dg.Children)
				{
					DarkenDrawing(d);
				}
			}
			else if (drawing is GeometryDrawing)
			{
				var gd = (GeometryDrawing)drawing;
				if (gd.Brush != null)
					DarkenBrush(gd.Brush);
				if (gd.Pen != null)
					DarkenBrush(gd.Pen.Brush);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		static void DarkenBrush(Brush brush)
		{
			if (brush is SolidColorBrush)
			{
				var b = (SolidColorBrush)brush;
				b.Color = DarkenColor(b.Color);
			}
			else if (brush is LinearGradientBrush)
			{
				var b = (LinearGradientBrush)brush;
				foreach (var stop in b.GradientStops)
					stop.Color = DarkenColor(stop.Color);
			}
			else if (brush is RadialGradientBrush)
			{
				var b = (RadialGradientBrush)brush;
				foreach (var stop in b.GradientStops)
					stop.Color = DarkenColor(stop.Color);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		static Color DarkenColor(Color color)
		{
			return Color.FromArgb(color.A, (byte)(color.R / 5), (byte)(color.G / 5), (byte)(color.B / 5));
		}
	}
}
