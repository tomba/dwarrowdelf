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
			foreach (var kvp in grid.GetIndexValueEnumerable())
			{
				var k = kvp.Key;
				var v = kvp.Value;

				/* clamp */
				if (v < average)
				{
					double d = average - v;
					v = average - Math.Pow(d, 1.0 / 20);
					grid[k] = v;
				}
			}
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

			foreach (var kvp in grid.GetIndexValueEnumerable())
			{
				var k = kvp.Key;
				var v = kvp.Value;

				v -= min;
				v /= d;
				grid[k] = v;
			}
		}
	}
}
