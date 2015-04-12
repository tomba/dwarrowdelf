using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public static class RayCastLerp
	{
		public static void Calculate(IntVector2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap, IntSize2 mapSize,
			Func<IntVector2, bool> blockerDelegate)
		{
			visibilityMap.Clear();

			if (blockerDelegate(viewerLocation) == true)
				return;

			var g = new IntGrid2(new IntVector2(), mapSize);
			g = g.Offset(-viewerLocation.X, -viewerLocation.Y);
			var vr = new IntVector2(visionRange, visionRange);
			g = g.Intersect(new IntGrid2(vr, -vr));

			int visionRangeSquared = (visionRange + 1) * (visionRange + 1);	// +1 to get a bit bigger view area

			foreach (var dst in g.Range())
			{
				if (dst.X * dst.X + dst.Y * dst.Y > visionRangeSquared)
					continue;

				bool vis = FindLos(viewerLocation, dst, blockerDelegate);
				visibilityMap[dst] = vis;
			}
		}

		static bool FindLos(IntVector2 viewerLocation, IntVector2 dst, Func<IntVector2, bool> blockerDelegate)
		{
			foreach (var p in LerpLine.PlotLine(new IntVector2(0, 0), dst))
			{
				if (p == dst)
					return true;

				if (blockerDelegate(viewerLocation + p) == true)
					return false;
			};

			return true;
		}

		public static void Calculate3(IntVector3 viewerLocation, int visionRange, Grid3D<bool> visibilityMap, IntSize3 mapSize,
				Func<IntVector3, bool> blockerDelegate)
		{
			visibilityMap.Clear();

			if (blockerDelegate(viewerLocation) == true)
				return;

			var g = new IntGrid3(new IntVector3(), mapSize);
			g = g.Offset(-viewerLocation.X, -viewerLocation.Y, -viewerLocation.Z);
			var vr = new IntVector3(visionRange, visionRange, visionRange);
			g = g.Intersect(new IntGrid3(vr, -vr));

			int visionRangeSquared = (visionRange + 1) * (visionRange + 1);	// +1 to get a bit bigger view area

			foreach (var dst in g.Range())
			{
				if (dst.LengthSquared > visionRangeSquared)
					continue;

				bool vis = FindLos3(viewerLocation, dst, blockerDelegate);
				visibilityMap[dst] = vis;
			}
		}

		static bool FindLos3(IntVector3 viewerLocation, IntVector3 dst, Func<IntVector3, bool> blockerDelegate)
		{
			foreach (var p in LerpLine.PlotLine3(new IntVector3(), dst))
			{
				if (p == dst)
					return true;

				if (blockerDelegate(viewerLocation + p) == true)
					return false;
			};

			return true;
		}
	}
}
