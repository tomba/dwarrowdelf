using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using System.Diagnostics;

namespace FOVTest
{
	struct FovData
	{
		public int id;
		public bool vis;
	}

	class NewFov
	{
		/*
		 *     .
		 *    ..
		 *   .#.
		 *  ..#.
		 * @....
		 * 
		 */
		public void Calculate(IntPoint2 viewerLocation, int visionRange, Grid2D<FovData> visibilityMap, IntSize2 mapSize,
			Func<IntPoint2, bool> blockerDelegate)
		{
			visionRange += 1; // visionrange does not include the tile where the observer is

			visibilityMap.Clear();

			if (blockerDelegate(new IntPoint2(0, 0) + (IntVector2)viewerLocation))
				return;

			var d = new FovData()
			{
				id = 0,
				vis = true,
			};

			visibilityMap[0, 0] = d;

			for (int octant = 0; octant < 1; ++octant)
				Calculate(viewerLocation, visionRange, visibilityMap, mapSize, blockerDelegate, 1, octant, 0.0, 1.0, 1);
		}

		void Calculate(IntPoint2 viewerLocation, int visionRange, Grid2D<FovData> visibilityMap, IntSize2 mapSize,
			Func<IntPoint2, bool> blockerDelegate, int startColumn, int octant, double startSlope, double endSlope, int id)
		{
			Debug.Print("Calc column {3}, id {0}, slope {1} -> {2}", id, startSlope, endSlope, startColumn);

			if (startSlope > endSlope)
				return;

			for (int x = startColumn; x < visionRange; ++x)
			{
				bool blocked = false;
				double newStart = 0;

				for (int y = 0; y <= x; ++y)
				{
					IntPoint2 translatedLocation = OctantTranslate(new IntPoint2(x, y), octant);
					IntPoint2 mapLocation = translatedLocation + (IntVector2)viewerLocation;

					if (mapSize.Contains(mapLocation) == false)
					{
						var nd = new FovData()
						{
							id = id,
							vis = false,
						};

						visibilityMap[translatedLocation] = nd;
						continue;
					}

					double lowerSlope = (y - 0.5) / (x + 0.5);
					double upperSlope = (y + 0.5) / (x - 0.5);

					if (x == 13)
						Debug.Print("{0},{1}   low {2:F2}, up {3:F2}", x, y, lowerSlope, upperSlope);

					//if (x == 13 && y == 8)
						//Debugger.Break();

					if (lowerSlope < 0)
						lowerSlope = 0;

					if (upperSlope > 1)
						upperSlope = 1;

					if (startSlope > lowerSlope)
						continue;

					if (endSlope < upperSlope)
						break;

					var d = new FovData()
					{
						id = id,
						vis = true,
					};

					visibilityMap[translatedLocation] = d;

					if (blocked)
					{
						if (blockerDelegate(mapLocation))
						{
							newStart = upperSlope;
							continue;
						}
						else
						{
							blocked = false;
							startSlope = newStart;
						}
					}
					else
					{
						if (blockerDelegate(mapLocation))
						{
							blocked = true;

							Calculate(viewerLocation, visionRange, visibilityMap, mapSize, blockerDelegate, x + 1, octant,
								startSlope, lowerSlope, id + 1);
							newStart = upperSlope;
						}
					}
				}

				if (blocked)
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
