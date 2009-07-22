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
		Drawing[] m_symbolDrawings;
		BitmapSource[] m_symbolBitmaps;
		BitmapSource[] m_symbolBitmapsDark;

		double m_size = 40;

		public double TileSize
		{
			get { return m_size; }

			set
			{
				m_size = value;
				for (int i = 0; i < m_symbolBitmaps.Length; ++i)
					m_symbolBitmaps[i] = null;
				for (int i = 0; i < m_symbolBitmapsDark.Length; ++i)
					m_symbolBitmapsDark[i] = null;
			}
		}

		public Drawing[] SymbolDrawings
		{
			get { return m_symbolDrawings; }

			set
			{
				m_symbolDrawings = value;
				m_symbolBitmaps = new BitmapSource[m_symbolDrawings.Length];
				m_symbolBitmapsDark = new BitmapSource[m_symbolDrawings.Length];
			}
		}

		public BitmapSource GetBitmap(int idx, bool dark)
		{
			if (dark)
			{
				if (m_symbolBitmapsDark[idx] == null)
					CreateSymbolBitmaps(idx);
				return m_symbolBitmapsDark[idx];
			}
			else
			{
				if (m_symbolBitmaps[idx] == null)
					CreateSymbolBitmaps(idx);

				return m_symbolBitmaps[idx];
			}
		}

		void CreateSymbolBitmaps(int idx)
		{
			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			Drawing d = m_symbolDrawings[idx];

			drawingContext.PushTransform(
				new ScaleTransform(Math.Floor(m_size) / 100, Math.Floor(m_size) / 100));

			drawingContext.DrawDrawing(d);
			drawingContext.Pop();

			drawingContext.Close();

			RenderTargetBitmap bmp = new RenderTargetBitmap((int)m_size, (int)m_size, 96, 96, PixelFormats.Default);
			bmp.Render(drawingVisual);
			bmp.Freeze();
			m_symbolBitmaps[idx] = bmp;

			drawingVisual.Opacity = 0.2;

			bmp = new RenderTargetBitmap((int)m_size, (int)m_size, 96, 96, PixelFormats.Default);
			bmp.Render(drawingVisual);
			bmp.Freeze();
			m_symbolBitmapsDark[idx] = bmp;
		}
	}
}
