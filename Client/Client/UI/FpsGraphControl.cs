using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	public class FpsGraphControl : Control
	{
		WriteableBitmap m_bmp;
		uint[] m_drawArray;

		TimeSpan m_lastRender;

		float[] m_fpsList = new float[128];
		int m_idx;

		public FpsGraphControl()
		{
			this.Loaded += FpsGraphControl_Loaded;
			this.Unloaded += FpsGraphControl_Unloaded;
		}

		void FpsGraphControl_Loaded(object sender, RoutedEventArgs e)
		{
			CompositionTarget.Rendering += CompositionTarget_Rendering;
		}

		void FpsGraphControl_Unloaded(object sender, RoutedEventArgs e)
		{
			CompositionTarget.Rendering -= CompositionTarget_Rendering;
		}

		void CompositionTarget_Rendering(object sender, EventArgs _e)
		{
			var e = (RenderingEventArgs)_e;

			var diff = e.RenderingTime - m_lastRender;

			if (diff == TimeSpan.Zero)
				return;

			m_lastRender = e.RenderingTime;

			m_fpsList[m_idx] = (float)diff.TotalMilliseconds;
			if (++m_idx == m_fpsList.Length)
				m_idx = 0;

			this.InvalidateVisual();
		}

		void UpdateBmp(int width, int height)
		{
			if (m_bmp == null || m_bmp.PixelWidth != width || m_bmp.PixelHeight != height)
			{
				m_bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
				m_drawArray = new uint[m_bmp.PixelWidth * m_bmp.PixelHeight];
			}
			else
			{
				Array.Clear(m_drawArray, 0, m_drawArray.Length);
			}

			m_bmp.Lock();

			float scale = height / 65.0f;
			
			for (int x = 0; x < width; ++x)
			{
				int idx = MyMath.Wrap(m_idx - width + x, m_fpsList.Length);

				float ms = m_fpsList[idx];
				if (ms == 0)
					continue;

				float fps = 1000.0f / ms;

				int barh = MyMath.Round(fps * scale);

				if (barh < 0)
					barh = 0;

				if (barh >= height)
					barh = height;

				for (int y = height - barh; y < height; ++y)
				{
					m_drawArray[y * m_bmp.BackBufferStride / 4 + x] = 0xff0000ff;
				}
			}
			
			m_bmp.WritePixels(new Int32Rect(0, 0, m_bmp.PixelWidth, m_bmp.PixelHeight), m_drawArray, m_bmp.BackBufferStride, 0);

			m_bmp.Unlock();
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			var size = this.RenderSize;

			if (size.IsEmpty)
				return;

			int w = (int)size.Width;
			int h = (int)size.Height;

			if (w == 0 || h == 0)
				return;

			UpdateBmp(w, h);

			drawingContext.DrawImage(m_bmp, new Rect(this.RenderSize));
		}
	}
}
