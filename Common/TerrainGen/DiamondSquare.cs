using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.TerrainGen
{
	public static class DiamondSquare
	{
		static Random s_random = new Random(1);

		public static void Render(Grid2D<double> grid, double average, double range, double h)
		{
			if (grid.Width != grid.Height)
				throw new Exception();

			foreach (var i in grid)
				grid[i.Key] = average;

			HeightMap(grid, average, range, h);
		}

		static double GetRandom(double range)
		{
			return s_random.NextDouble() * range * 2 - range;
		}

		static void HeightMap(Grid2D<double> grid, double average, double range, double h)
		{
			for (int pass = 0; pass < (int)Math.Log(grid.Width, 2); ++pass)
			{
				var parts = (int)Math.Pow(2, pass);
				var size = (grid.Width - 1) / parts;

				if (size == 0)
					throw new Exception();

				for (int y = 0; y < parts; ++y)
				{
					for (int x = 0; x < parts; ++x)
					{
						var rect = new IntRect(size * x, size * y, size, size);
						int radius = rect.Width / 2;

						if (radius == 0)
							throw new Exception();

						var middle = rect.X1Y1 + new IntVector(radius, radius);

						Rectangle(grid, middle, radius, range);

						var mxy1 = new IntPoint(middle.X, rect.Y1);
						var mxy2 = new IntPoint(middle.X, rect.Y2);
						var x1my = new IntPoint(rect.X1, middle.Y);
						var x2my = new IntPoint(rect.X2, middle.Y);

						Diamond(grid, x1my, radius, range, average);
						Diamond(grid, x2my, radius, range, average);
						Diamond(grid, mxy1, radius, range, average);
						Diamond(grid, mxy2, radius, range, average);
					}
				}

				range *= Math.Pow(2, -h);
			}
		}

		static void Rectangle(Grid2D<double> grid, IntPoint middle, int radius, double range)
		{
			double v1, v2, v3, v4;
			IntPoint p1, p2, p3, p4;

			p1 = middle.Offset(radius, radius);
			v1 = grid[p1];

			p2 = middle.Offset(-radius, radius);
			v2 = grid[p2];

			p3 = middle.Offset(radius, -radius);
			v3 = grid[p3];

			p4 = middle.Offset(-radius, -radius);
			v4 = grid[p4];

			grid[middle] = (v1 + v2 + v3 + v4) / 4 + GetRandom(range);
		}

		static void Diamond(Grid2D<double> grid, IntPoint middle, int radius, double range, double average)
		{
			double v1, v2, v3, v4;
			IntPoint p1, p2, p3, p4;

			p1 = middle.Offset(0, -radius);
			v1 = grid.Bounds.Contains(p1) ? grid[p1] : average;

			p2 = middle.Offset(-radius, 0);
			v2 = grid.Bounds.Contains(p2) ? grid[p2] : average;

			p3 = middle.Offset(0, radius);
			v3 = grid.Bounds.Contains(p3) ? grid[p3] : average;

			p4 = middle.Offset(radius, 0);
			v4 = grid.Bounds.Contains(p4) ? grid[p4] : average;

			grid[middle] = (v1 + v2 + v3 + v4) / 4 + GetRandom(range);
		}
	}
}
