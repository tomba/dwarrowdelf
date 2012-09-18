using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using System.Diagnostics;

namespace FOVTest
{
	class NewFov : ILOSAlgo
	{
		public void Calculate(IntPoint2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap, IntSize2 mapSize,
			Func<IntPoint2, bool> blockerDelegate)
		{
			visionRange += 1; // visionrange does not include the tile where the observer is

			visibilityMap.Clear();

			if (blockerDelegate(new IntPoint2(0, 0) + (IntVector2)viewerLocation))
				return;

			visibilityMap[0, 0] = true;

			for (int octant = 0; octant < 8; ++octant)
				Calculate(viewerLocation, visionRange, visibilityMap, mapSize, blockerDelegate, 1, octant, 0.0, 1.0, 1);
		}

		void Calculate(IntPoint2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap, IntSize2 mapSize,
			Func<IntPoint2, bool> blockerDelegate, int startColumn, int octant, double startSlope, double endSlope, int id)
		{
			//Debug.Print("{4}{0}: Calc, start col {3}, slope {1} -> {2}", id, startSlope, endSlope, startColumn, new string(' ', (id - 1) * 4));

			if (startSlope > endSlope)
				return;

			for (int x = startColumn; x < visionRange; ++x)
			{
				bool currentlyBlocked = false;
				double newStart = 0;

				int lowY = (int)Math.Floor(startSlope * x);
				int highY = (int)Math.Ceiling(endSlope * x);

				//Debug.Print("{0}{1}: col {2} lowY {3}, highY {4}", new string(' ', (id - 1) * 4), id, x, lowY, highY);

				for (int y = lowY; y <= highY; ++y)
				{
					IntPoint2 translatedLocation = OctantTranslate(new IntPoint2(x, y), octant);
					IntPoint2 mapLocation = translatedLocation + (IntVector2)viewerLocation;

					if (mapSize.Contains(mapLocation) == false || new IntVector2(x, y).Length > visionRange)
					{
						visibilityMap[translatedLocation] = false;
						continue;
					}

					double centerSlope = (double)y / x;

					if (centerSlope < startSlope || centerSlope > endSlope)
					{
						//Debug.Print("{0}{1}: {2},{3}   center {4:F2} ouside arc", new string(' ', (id - 1) * 4), id, x, y, centerSlope);
						continue;
					}

					//Debug.Print("{0}{1}: {2},{3}   center {4:F2} visible", new string(' ', (id - 1) * 4), id, x, y, centerSlope);

					visibilityMap[translatedLocation] = true;

					bool tileBlocked = blockerDelegate(mapLocation);

					double upperSlope = (y + 0.5) / (x - 0.5);

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

							double lowerSlope = (y - 0.5) / (x + 0.5);

							Calculate(viewerLocation, visionRange, visibilityMap, mapSize, blockerDelegate, x + 1, octant,
								startSlope, lowerSlope, id + 1);
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

		static IntPoint2 OctantTranslate(IntPoint2 p, int octant)
		{
			int tx = p.X * xxcomp[octant] + p.Y * xycomp[octant];
			int ty = p.X * yxcomp[octant] + p.Y * yycomp[octant];

			return new IntPoint2(tx, ty);
		}
	}
}
