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

		public double Average { get; private set; }

		public void Generate(DiamondSquare.CornerData corners, double range, double h, int seed)
		{
			m_grid.Clear();

			GenerateTerrain(m_grid, corners, range, h, seed);

			AnalyzeTerrain(m_grid);

			RenderTerrain(m_grid);
		}

		void GenerateTerrain(ArrayGrid2D<double> grid, DiamondSquare.CornerData corners, double range, double h, int seed)
		{
			DiamondSquare.Render(grid, corners, range, h, seed);

			//Clamper.Clamp(grid, 10);
		}

		void AnalyzeTerrain(ArrayGrid2D<double> grid)
		{
			Clamper.Normalize(grid);
			this.Average = grid.Average();
		}

		void RenderTerrain(ArrayGrid2D<double> grid)
		{
			uint[] array = new uint[grid.Width];

			m_bmp.Lock();

			for (int y = 0; y < grid.Height && y < m_bmp.PixelHeight; ++y)
			{
				for (int x = 0; x < grid.Width && x < m_bmp.PixelWidth; ++x)
				{
					var v = grid[x, y];

					var c = GetColor(v);

					array[x] = c;
				}

				m_bmp.WritePixels(new Int32Rect(0, y, grid.Width, 1), array, grid.Width * 4, 0);
			}

			m_bmp.AddDirtyRect(new Int32Rect(0, 0, m_bmp.PixelWidth, m_bmp.PixelHeight));
			m_bmp.Unlock();
		}

		uint GetColor(double v)
		{
			uint c = (uint)(v * 255);

			if (c < 0 || c > 255)
				throw new Exception();

			uint r, g, b;

			if (v >= 0.50)
			{
				r = c;
				g = c;
				b = c;
			}
			else if (v >= 0.20)
			{
				r = 0;
				g = c;
				b = 0;
			}
			else
			{
				r = 0;
				g = 0;
				b = 150;
			}

			return (r << 16) | (g << 8) | (b << 0);
		}
	}
}
