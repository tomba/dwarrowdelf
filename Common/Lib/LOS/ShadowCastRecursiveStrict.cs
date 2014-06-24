//#define PERMISSIVE
#define MEDIUM_STRICT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public static class ShadowCastRecursiveStrict
	{
		struct SCRData
		{
			public IntVector2 ViewerLocation;
			public int VisionRange;
			public int VisionRangeSquared;
			public Grid2D<bool> VisibilityMap;
			public IntSize2 MapSize;
			public Func<IntVector2, bool> BlockerDelegate;
		}

		public static void Calculate(IntVector2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap, IntSize2 mapSize,
			Func<IntVector2, bool> blockerDelegate)
		{
			visibilityMap.Clear();

			if (blockerDelegate(viewerLocation) == true)
				return;

			visibilityMap[0, 0] = true;

			SCRData data = new SCRData()
			{
				ViewerLocation = viewerLocation,
				VisionRange = visionRange,
				VisionRangeSquared = (visionRange + 1) * (visionRange + 1),	// +1 to get a bit bigger view area
				VisibilityMap = visibilityMap,
				MapSize = mapSize,
				BlockerDelegate = blockerDelegate,
			};

			for (int octant = 0; octant < 8; ++octant)
				Calculate(ref data, 1, octant, 0.0, 1.0, 1);
		}

		static void Calculate(ref SCRData data, int startColumn, int octant, double startSlope, double endSlope, int id)
		{
			//Debug.Print("{4}{0}: Calc, start col {3}, slope {1} -> {2}", id, startSlope, endSlope, startColumn, new string(' ', (id - 1) * 4));

			if (startSlope > endSlope)
				return;

			int maxX;

			switch (octant)
			{
				case 0:
				case 7:
					maxX = Math.Min(data.VisionRange, data.MapSize.Width - data.ViewerLocation.X - 1);
					break;

				case 1:
				case 2:
					maxX = Math.Min(data.VisionRange, data.MapSize.Height - data.ViewerLocation.Y - 1);
					break;

				case 3:
				case 4:
					maxX = Math.Min(data.VisionRange, data.ViewerLocation.X);
					break;

				case 5:
				case 6:
					maxX = Math.Min(data.VisionRange, data.ViewerLocation.Y);
					break;

				default:
					throw new Exception();
			}

			for (int x = startColumn; x <= maxX; ++x)
			{
				bool currentlyBlocked = false;
				double newStart = 0;

				int lowY = MyMath.RoundTowards(startSlope * x);
				int highY = MyMath.RoundAway(endSlope * x);

				switch (octant)
				{
					case 0:
					case 3:
						highY = Math.Min(highY, data.MapSize.Height - data.ViewerLocation.Y - 1);
						break;

					case 1:
					case 6:
						highY = Math.Min(highY, data.MapSize.Width - data.ViewerLocation.X - 1);
						break;

					case 7:
					case 4:
						highY = Math.Min(highY, data.ViewerLocation.Y);
						break;

					case 2:
					case 5:
						highY = Math.Min(highY, data.ViewerLocation.X);
						break;

					default:
						throw new Exception();
				}

				//Debug.Print("{0}{1}: col {2} lowY {3}, highY {4}", new string(' ', (id - 1) * 4), id, x, lowY, highY);

				for (int y = lowY; y <= highY; ++y)
				{
					IntVector2 translatedLocation = OctantTranslate(new IntVector2(x, y), octant);
					IntVector2 mapLocation = translatedLocation.Offset(data.ViewerLocation.X, data.ViewerLocation.Y);

					Debug.Assert(data.MapSize.Contains(mapLocation));

					if (x * x + y * y > data.VisionRangeSquared)
					{
						data.VisibilityMap[translatedLocation] = false;
						continue;
					}

					double lowerSlope = (y - 0.5) / (x + 0.5);
					double upperSlope = (y + 0.5) / (x - 0.5);

#if PERMISSIVE
					if (upperSlope < startSlope || lowerSlope > endSlope)
					{
						//Debug.Print("{0}{1}: {2},{3}   center {4:F2} ouside arc", new string(' ', (id - 1) * 4), id, x, y, centerSlope);
						continue;
					}

					bool tileBlocked = data.BlockerDelegate(mapLocation);
#elif MEDIUM_STRICT
					double centerSlope = (double)y / x;

					bool tileBlocked = data.BlockerDelegate(mapLocation);

					if (!tileBlocked && (centerSlope < startSlope || centerSlope > endSlope))
					{
						//Debug.Print("{0}{1}: {2},{3}   center {4:F2} ouside arc", new string(' ', (id - 1) * 4), id, x, y, centerSlope);
						continue;
					}
#else
#error no mode defined
#endif
					//Debug.Print("{0}{1}: {2},{3}   center {4:F2} visible", new string(' ', (id - 1) * 4), id, x, y, centerSlope);

					data.VisibilityMap[translatedLocation] = true;

					if (currentlyBlocked)
					{
						if (tileBlocked)
						{
							newStart = upperSlope;
							continue;
						}
						else
						{
							currentlyBlocked = false;
							startSlope = newStart;
							//Debug.Print("{0}{1}: {2},{3}  new startSlope {4:F2}", new string(' ', (id - 1) * 4), id, x, y, startSlope);
						}
					}
					else
					{
						if (tileBlocked)
						{
							currentlyBlocked = true;
							newStart = upperSlope;

							Calculate(ref data, x + 1, octant, startSlope, lowerSlope, id + 1);
						}
					}
				}

				if (currentlyBlocked)
					break;
			}
		}

		static readonly int[] xxcomp = { 1, 0, 0, -1, -1, 0, 0, 1 };
		static readonly int[] xycomp = { 0, 1, -1, 0, 0, -1, 1, 0 };
		static readonly int[] yxcomp = { 0, 1, 1, 0, 0, -1, -1, 0 };
		static readonly int[] yycomp = { 1, 0, 0, 1, -1, 0, 0, -1 };

		static IntVector2 OctantTranslate(IntVector2 p, int octant)
		{
			int tx = p.X * xxcomp[octant] + p.Y * xycomp[octant];
			int ty = p.X * yxcomp[octant] + p.Y * yycomp[octant];

			return new IntVector2(tx, ty);
		}
	}
}
