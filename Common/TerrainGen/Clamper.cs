using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.TerrainGen
{
	public static class Clamper
	{
		public static void Clamp(ArrayGrid2D<double> grid, double average)
		{
			grid.ForEach(v =>
			{
				if (v < average)
				{
					double d = average - v;
					v = average - Math.Pow(d, 1.0 / 20);
				}

				return v;
			});
		}

		public static void MinMax(ArrayGrid2D<double> grid, out double min, out double max)
		{
			max = Double.MinValue;
			min = Double.MaxValue;

			foreach (var v in grid)
			{
				if (v < min)
					min = v;
				if (v > max)
					max = v;
			}
		}

		public static void Normalize(ArrayGrid2D<double> grid)
		{
			double min, max;

			MinMax(grid, out min, out max);

			double d = max - min;

			grid.ForEach(v =>
			{
				v -= min;
				v /= d;
				return v;
			});
		}
	}
}
