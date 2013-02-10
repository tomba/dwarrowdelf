using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public static class RayCast
	{
		static bool FindLos(IntPoint2 src, IntPoint2 dst, Func<IntPoint2, bool> blockerDelegate)
		{
			bool vis = true;

			Line(src.X, src.Y, dst.X, dst.Y, (x, y) =>
				{
					var p = new IntPoint2(x, y);

					if (p == dst)
						return true;

					if (blockerDelegate(p) == true)
					{
						vis = false;
						return false;
					}

					return true;
				});

			return vis;
		}

		public static void Calculate(IntPoint2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap, IntSize2 mapSize,
			Func<IntPoint2, bool> blockerDelegate)
		{
			visibilityMap.Clear();

			if (blockerDelegate(viewerLocation) == true)
				return;

			for (int y = -visionRange; y <= visionRange; ++y)
			{
				for (int x = -visionRange; x <= visionRange; ++x)
				{
					var dst = viewerLocation + new IntVector2(x, y);

					if (mapSize.Contains(dst) == false)
					{
						visibilityMap[x, y] = false;
						continue;
					}

					bool vis = FindLos(viewerLocation, dst, blockerDelegate);
					visibilityMap[x, y] = vis;
				}
			}
		}

		private static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }

		/// <summary>
		/// The plot function delegate
		/// </summary>
		/// <param name="x">The x co-ord being plotted</param>
		/// <param name="y">The y co-ord being plotted</param>
		/// <returns>True to continue, false to stop the algorithm</returns>
		public delegate bool PlotFunction(int x, int y);

		/// <summary>
		/// Plot the line from (x0, y0) to (x1, y10
		/// </summary>
		/// <param name="x0">The start x</param>
		/// <param name="y0">The start y</param>
		/// <param name="x1">The end x</param>
		/// <param name="y1">The end y</param>
		/// <param name="plot">The plotting function (if this returns false, the algorithm stops early)</param>
		public static void Line(int x0, int y0, int x1, int y1, PlotFunction plot)
		{
			bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
			if (steep) { Swap<int>(ref x0, ref y0); Swap<int>(ref x1, ref y1); }
			if (x0 > x1) { Swap<int>(ref x0, ref x1); Swap<int>(ref y0, ref y1); }
			int dX = (x1 - x0), dY = Math.Abs(y1 - y0), err = (dX / 2), ystep = (y0 < y1 ? 1 : -1), y = y0;

			for (int x = x0; x <= x1; ++x)
			{
				if (!(steep ? plot(y, x) : plot(x, y)))
					return;
				err = err - dY;
				if (err < 0) { y += ystep; err += dX; }
			}
		}
	}
}
