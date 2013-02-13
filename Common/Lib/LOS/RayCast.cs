using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public static class RayCast
	{
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

		static bool FindLos(IntPoint2 src, IntPoint2 dst, Func<IntPoint2, bool> blockerDelegate)
		{
			bool vis = true;

			Bresenhams.PlotLine(src, dst, (p) =>
			{
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
	}
}
