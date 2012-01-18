using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf;
using System.Windows;
using System.Windows.Media;
using Dwarrowdelf.TerrainGen;
using System.Diagnostics;

namespace TerrainGenTest
{
	class TerrainWriter
	{
		WriteableBitmap m_bmp;
		ArrayGrid2D<double> m_grid;

		public BitmapSource Bmp { get { return m_bmp; } }

		public TerrainWriter()
		{
			int sizeExp = 9;
			int size = (int)Math.Pow(2, sizeExp) + 1;
			m_grid = new ArrayGrid2D<double>(size, size);

			m_bmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgr32, null);
		}

		public TimeSpan Time { get; private set; }
		public double Min { get; private set; }
		public double Max { get; private set; }

		public void Generate(DiamondSquare.CornerData corners, double range, double h, int seed)
		{
			m_grid.Clear();

			GenerateTerrain(m_grid, corners, range, h, seed);

			RenderTerrain(m_grid);
		}

		void GenerateTerrain(ArrayGrid2D<double> grid, DiamondSquare.CornerData corners, double range, double h, int seed)
		{
			DiamondSquare.Render(grid, corners, range, h, seed);

			//Clamper.Clamp(grid, 10);
		}

		void RenderTerrain(ArrayGrid2D<double> grid)
		{
			double min, max;
			Clamper.MinMax(grid, out min, out max);

			this.Min = min;
			this.Max = max;

			var diff = max - min;
			var mul = 255 / diff;

			uint[] array = new uint[grid.Width];

			m_bmp.Lock();

			for (int y = 0; y < grid.Height && y < m_bmp.PixelHeight; ++y)
			{
				for (int x = 0; x < grid.Width && x < m_bmp.PixelWidth; ++x)
				{
					var v = grid[x, y];

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

				m_bmp.WritePixels(new Int32Rect(0, y, grid.Width, 1), array, grid.Width * 4, 0);
			}

			m_bmp.AddDirtyRect(new Int32Rect(0, 0, m_bmp.PixelWidth, m_bmp.PixelHeight));
			m_bmp.Unlock();
		}

	}
}
