using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public class TerrainGen
	{
		Random s_random = new Random(1);

		public Grid2D<double> Grid { get; private set; }

		double m_range = 5;			/* values from m_average +/- m_range */
		double m_average = 10.0;	/* average level */
		double m_h = 0.75;			/* jaggedness */

		public double Min { get; private set; }
		public double Max { get; private set; }

		public TerrainGen(int sizeExp, double average, double range, double h)
		{
			m_average = average;
			m_range = range;
			m_h = h;

			var grid = new Grid2D<double>((int)Math.Pow(2, sizeExp) + 1, (int)Math.Pow(2, sizeExp) + 1);
			foreach (var i in grid)
				grid[i.Key] = m_average;
			HeightMap(grid);

			double max = Double.MinValue, min = Double.MaxValue;

			foreach (var kvp in grid)
			{
				var k = kvp.Key;
				var v = kvp.Value;

				/* clamp */
				if (v < m_average)
				{
					double d = m_average - v;
					v = m_average - Math.Pow(d, 1.0 / 20);
					grid[kvp.Key] = v;
				}

				if (v < min)
					min = v;
				if (v > max)
					max = v;
			}

			this.Min = min;
			this.Max = max;

			//MyDebug.WriteLine("min {0}, max {1}", min, max);
			this.Grid = grid;
		}

		double GetRandom(double randomCoef)
		{
			return (s_random.NextDouble() * m_range * 2 - m_range) * randomCoef;
		}

		void HeightMap(Grid2D<double> grid)
		{
			HeightMap(grid, new IntRect(0, 0, grid.Width - 1, grid.Height - 1), 1.0);
		}

		void HeightMap(Grid2D<double> grid, IntRect rect, double randomCoef)
		{
			Debug.Assert(rect.Width == rect.Height);

			int r = rect.Width / 2;

			if (r == 0)
				return;

			var mp = rect.X1Y1 + new IntVector(r, r);

			var x1y1 = new IntPoint(rect.X1, rect.Y1);
			var x2y1 = new IntPoint(rect.X2, rect.Y1);
			var x1y2 = new IntPoint(rect.X1, rect.Y2);
			var x2y2 = new IntPoint(rect.X2, rect.Y2);

			var mxy1 = new IntPoint(mp.X, rect.Y1);
			var mxy2 = new IntPoint(mp.X, rect.Y2);
			var x1my = new IntPoint(rect.X1, mp.Y);
			var x2my = new IntPoint(rect.X2, mp.Y);

			double v1, v2, v3, v4;

			v1 = grid[x1y1];
			v2 = grid[x2y1];
			v3 = grid[x1y2];
			v4 = grid[x2y2];

			grid[mp] = (v1 + v2 + v3 + v4) / 4 + GetRandom(randomCoef);

			Diamond(grid, x1my, r, randomCoef);
			Diamond(grid, x2my, r, randomCoef);
			Diamond(grid, mxy1, r, randomCoef);
			Diamond(grid, mxy2, r, randomCoef);

			randomCoef = randomCoef * Math.Pow(2, -m_h);

			HeightMap(grid, new IntRect(x1y1, mp), randomCoef);
			HeightMap(grid, new IntRect(x2y1, mp), randomCoef);
			HeightMap(grid, new IntRect(x1y2, mp), randomCoef);
			HeightMap(grid, new IntRect(x2y2, mp), randomCoef);
		}

		void Diamond(Grid2D<double> grid, IntPoint p, int r, double randomCoef)
		{
			double v1, v2, v3, v4;
			IntPoint p1, p2, p3, p4;

			p1 = p + new IntVector(0, -r);
			v1 = grid.Bounds.Contains(p1) ? grid[p1] : m_average;

			p2 = p + new IntVector(-r, 0);
			v2 = grid.Bounds.Contains(p2) ? grid[p2] : m_average;

			p3 = p + new IntVector(0, r);
			v3 = grid.Bounds.Contains(p3) ? grid[p3] : m_average;

			p4 = p + new IntVector(r, 0);
			v4 = grid.Bounds.Contains(p4) ? grid[p4] : m_average;

			grid[p] = (v1 + v2 + v3 + v4) / 4 + GetRandom(randomCoef);
		}
	}
}
