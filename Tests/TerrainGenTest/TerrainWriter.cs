using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf;
using System.Windows;
using System.Windows.Media;
using Dwarrowdelf.TerrainGen;

namespace TerrainGenTest
{
	class TerrainWriter
	{
		WriteableBitmap m_bmp;
		Grid2D<double> m_grid;

		public WriteableBitmap Bmp { get; private set; }

		public TerrainWriter()
		{
			int sizeExp = 9;
			int size = (int)Math.Pow(2, sizeExp) + 1;
			m_grid = new Grid2D<double>(size, size);

			m_bmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgr32, null);
			this.Bmp = m_bmp;
		}

		public TimeSpan Time { get; private set; }

		public void Do(double range = 5, double h = 0.75)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();
			double average = 10;

			DiamondSquare.Render(m_grid, average, range, h);

			sw.Stop();
			this.Time = sw.Elapsed;

			Clamper.Clamp(m_grid, average);

			double min, max;
			Clamper.MinMax(m_grid, out min, out max);

			
			var diff = max - min;
			var mul = 255 / diff;

			uint[] array = new uint[m_grid.Width];

			m_bmp.Lock();

			for (int y = 0; y < m_grid.Height; ++y)
			{
				if (y >= m_bmp.PixelHeight)
						continue;

				for (int x = 0; x < m_grid.Width; ++x)
				{
					if (x >= m_bmp.PixelWidth)
						continue;

					var v = m_grid[new IntPoint(x, y)];

					v -= min;
					v *= mul;
					v = Math.Round(v);
					if (v < 0 || v > 255)
						throw new Exception();

					var r = (uint)v;
					var g = (uint)v;
					var b = (uint)v;

					array[x] = (r << 16) | (g << 8) | (b << 0);
				}

				m_bmp.WritePixels(new Int32Rect(0, y, m_grid.Width, 1), array, m_grid.Width * 4, 0);
			}

			m_bmp.AddDirtyRect(new Int32Rect(0, 0, m_grid.Width, m_grid.Height));
			m_bmp.Unlock();
		}
	}
}
