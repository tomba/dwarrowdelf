﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.TerrainGen
{
	public static class Clamper
	{
		public static void Clamp(Grid2D<double> grid, double average)
		{
			foreach (var kvp in grid)
			{
				var k = kvp.Key;
				var v = kvp.Value;

				/* clamp */
				if (v < average)
				{
					double d = average - v;
					v = average - Math.Pow(d, 1.0 / 20);
					grid[kvp.Key] = v;
				}
			}
		}

		public static void MinMax(Grid2D<double> grid, out double min, out double max)
		{
			max = Double.MinValue;
			min = Double.MaxValue;

			foreach (var kvp in grid)
			{
				var k = kvp.Key;
				var v = kvp.Value;

				if (v < min)
					min = v;
				if (v > max)
					max = v;
			}
		}
	}
}