using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dwarrowdelf.Client.TileControl
{
	public interface ISymbolDrawingCache
	{
		Drawing GetDrawing(SymbolID symbolID, GameColor color);
	}

	public class SymbolBitmapCache : IBitmapGenerator
	{
		ISymbolDrawingCache m_symbolDrawingCache;

		BitmapSource[] m_nonColoredBitmapList;
		Dictionary<GameColor, BitmapSource>[] m_coloredBitmapMap;

		int m_size;
		int m_numDistinctBitmaps;

		public SymbolBitmapCache(ISymbolDrawingCache symbolDrawingCache, int size)
		{
			if (size == 0)
				throw new Exception();

			m_symbolDrawingCache = symbolDrawingCache;
			m_size = size;

			var arr = (SymbolID[])Enum.GetValues(typeof(SymbolID));
			var max = (int)arr.Max() + 1;
			m_numDistinctBitmaps = max;

			m_nonColoredBitmapList = new BitmapSource[m_numDistinctBitmaps];
			m_coloredBitmapMap = new Dictionary<GameColor, BitmapSource>[m_numDistinctBitmaps];
		}

		public void Invalidate()
		{
			m_nonColoredBitmapList = new BitmapSource[m_numDistinctBitmaps];
			m_coloredBitmapMap = new Dictionary<GameColor, BitmapSource>[m_numDistinctBitmaps];
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

		public BitmapSource GetBitmap(SymbolID symbolID, GameColor color)
		{
			BitmapSource bitmap;

			if (color == GameColor.None)
			{
				bitmap = m_nonColoredBitmapList[(int)symbolID];

				if (bitmap == null)
				{
					bitmap = CreateSymbolBitmap(symbolID, color);
					m_nonColoredBitmapList[(int)symbolID] = bitmap;
				}
			}
			else
			{
				var map = m_coloredBitmapMap[(int)symbolID];

				if (map == null)
				{
					map = new Dictionary<GameColor, BitmapSource>();
					m_coloredBitmapMap[(int)symbolID] = map;
				}

				if (!map.TryGetValue(color, out bitmap))
				{
					bitmap = CreateSymbolBitmap(symbolID, color);
					map[color] = bitmap;
				}
			}

			return bitmap;
		}

		BitmapSource CreateSymbolBitmap(SymbolID symbolID, GameColor color)
		{
			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			var d = m_symbolDrawingCache.GetDrawing(symbolID, color);

			drawingContext.PushTransform(new ScaleTransform((double)m_size / 100, (double)m_size / 100));
			drawingContext.DrawDrawing(d);
			drawingContext.Pop();
			drawingContext.Close();

			RenderTargetBitmap bmp = new RenderTargetBitmap(m_size, m_size, 96, 96, PixelFormats.Default);
			bmp.Render(drawingVisual);
			bmp.Freeze();

			return bmp;
		}
	}
}
