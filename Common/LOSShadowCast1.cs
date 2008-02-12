using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	// http://sc.tri-bit.com/Computing_LOS_for_Large_Areas
	public static class LOSShadowCast1
	{
		class LOSCell // xxx struct?
		{
			public double upperShadowCount;
			public double upperShadowMax;
			public double lowerShadowCount;
			public double lowerShadowMax;
			public bool visible;
			public bool lit;
			public bool litDelay;

			public void Initialize()
			{
				upperShadowCount = 0;
				upperShadowMax = 0;
				lowerShadowCount = 0;
				lowerShadowMax = 0;
				visible = true;
				lit = true;
				litDelay = false;
			}
		}

		static LOSCell[] cells;

		// blockerDelegate(location) returns if tile at location is a blocker. slow?
		static public void CalculateLOS(Location viewerLocation, int visionRange, LocationGrid<bool> visibilityMap, 
			IntRect mapBounds, Func<Location, bool> blockerDelegate)
		{
			visionRange += 1; // visionrange does not include the tile where the observer is

			if (cells == null || cells.Length < visionRange)
			{
				cells = new LOSCell[visionRange];
				for (int i = 0; i < visionRange; i++)
					cells[i] = new LOSCell();
			}

			for (int i = 0; i < 8; i++)
				CalculateLOS(viewerLocation, visionRange, visibilityMap, mapBounds, blockerDelegate, i);
		}

		static void CalculateLOS(Location viewerLocation, int visionRange, LocationGrid<bool> visibilityMap,
			IntRect mapBounds, Func<Location, bool> blockerDelegate, int octant)
		{
			// Cell (0,0) is assumed to be lit and visible in all cases.
			cells[0].Initialize();

			//bool visibleCorner = false;

			for (int x = 0; x < visionRange; x++)
			{
				for (int y = 0; y <= x; y++)
				{
					Location translatedLocation = OctantTranslate(new Location(x, y), octant);
					Location mapLocation = translatedLocation + viewerLocation;

					LOSCell cell = cells[y];
					LOSCell cellS = null;
					if (y > 0)
						cellS = cells[y - 1];

					// does the current cell represent a grid square that blocks LOS?
					bool blocker;

					if (mapBounds.Contains(mapLocation))
					{
						if (blockerDelegate(mapLocation))
							blocker = true;
						else
							blocker = false;
					}
					else
						blocker = true;

					double upInc = 1;
					double lowInc = 1;

					// STEP 1 - inherit values from immediately preceeding column
					//          light up from lit_delay if appropriate
					//          'steal' lower bound shadow from 'south' cell if
					//          if it lit
					if (y < x)
					{
						if (cell.litDelay)
						{
							if (!blocker)
							{
								if (y > 0 && cellS.lit)
								{
									if (cellS.lowerShadowMax != 0)
									{
										cell.lit = false;
										cell.lowerShadowMax = cellS.lowerShadowMax;
										cell.lowerShadowCount = cellS.lowerShadowCount;
										cellS.lowerShadowMax = 0;
										cellS.lowerShadowCount = 0;
										lowInc = 0;
									}
									else
									{
										cell.lit = true;
									}
								}
							}

							cell.litDelay = false;
						}
					}
					else
					{
						cell.Initialize();
					}

					// STEP 2 - check for blocker
					//          a dark blocker in a shadows edge will be visible
					//          (but still dark)
					if (blocker)
					{
						if (cell.lit || (y > 0 && cellS.lit))// || visibleCorner)
						{
							//visibleCorner = cell.lit;
							cell.lit = false;	// blockers are always dark
							cell.visible = true;	// but always visible if we get here..

							double upper;
							{
								double x1 = 0; double y1 = 0; double x2 = x - 0.5; double y2 = y + 0.5;
								double k = (y2 - y1) / (x2 - x1);
								double ik = 1 / k;
								upper = ik;
							}

							double lower;
							{
								double x1 = 0; double y1 = 0; double x2 = x + 0.5; double y2 = y - 0.5;
								double k = (y2 - y1) / (x2 - x1);
								double ik = 1 / k;
								lower = ik;
								if (lower < 0)
									lower = 1000; // xxx?
							}

							if (upper < cell.upperShadowMax || cell.upperShadowMax == 0)
							{
								// new upper shadow
								cell.upperShadowMax = upper;
								cell.upperShadowCount = 0;
								upInc = 0;
							}

							if (lower > cell.lowerShadowMax || cell.lowerShadowMax == 0)
							{
								// new lower shadow
								cell.lowerShadowMax = lower;
								cell.lowerShadowCount = -1;
								lowInc = 0;
								if (lower <= 3)		// somewhat arbitrary, but looks right
									cell.litDelay = true;
							}
						}
						else
						{
							cell.visible = false;
						}
					}
					else
					{
						cell.visible = false;
					}


					// STEP 3 - add increments to upper and lower counts
					cell.upperShadowCount += upInc;
					cell.lowerShadowCount += lowInc;

					// STEP 4 - look south to see if we've been overtaken by shadow
					if (y > 0)
					{
						if (CellReachedUpMax(cellS))
						{
							if (!CellReachedUpMax(cell))
							{
								cell.upperShadowMax = cellS.upperShadowMax;
								cell.upperShadowCount = cellS.upperShadowCount;
								cell.upperShadowCount -= cellS.upperShadowMax;
								cellS.upperShadowMax = 0;
								cellS.upperShadowCount = 0;
							}

							cell.lit = false;
							cell.visible = false;
						}

						// STEP 5 - erase current lower shadow if one is active in the
						//          cell to our south
						if (CellReachedLowMax(cellS))
						{
							cell.lowerShadowMax = cellS.lowerShadowMax;
							cell.lowerShadowCount = cellS.lowerShadowCount;
							cell.lowerShadowCount -= cellS.lowerShadowMax;
							cellS.lowerShadowMax = 0;
							cellS.lowerShadowCount = 0;
						}

						if (cellS.lowerShadowMax != 0 || (cellS.lowerShadowMax == 0 && !cellS.lit))
							cell.lowerShadowCount = cell.lowerShadowMax + 10;

					}


					// STEP 6 - light up if we've reached lower max (ie come out of shadow)
					if (CellReachedLowMax(cell))
						cell.lit = true;

					// STEP 7 - apply 'lit' value
					if (mapBounds.Contains(mapLocation))
					{
						if (cell.lit || (blocker && cell.visible))
							visibilityMap[translatedLocation] = true;
						else
							visibilityMap[translatedLocation] = false;
					}
					else
						visibilityMap[translatedLocation] = false;
				}
			}
		}

		static bool CellReachedUpMax(LOSCell cell)
		{
			if (cell.upperShadowMax != 0 &&
				cell.upperShadowCount + 0.5 >= cell.upperShadowMax)// &&
				//cell.upperShadowCount - 0.5 <= cell.upperShadowMax)
				return true;
			else
				return false;
		}

		static bool CellReachedLowMax(LOSCell cell)
		{
			if (cell.lowerShadowMax != 0 &&
				cell.lowerShadowCount + 0.5 >= cell.lowerShadowMax &&
				cell.lowerShadowCount - 0.5 <= cell.lowerShadowMax)
				return true;
			else
				return false;
		}

		static readonly int[] xxcomp = { 1, 0, 0, -1, -1, 0, 0, 1 };
		static readonly int[] xycomp = { 0, 1, -1, 0, 0, -1, 1, 0 };
		static readonly int[] yxcomp = { 0, 1, 1, 0, 0, -1, -1, 0 };
		static readonly int[] yycomp = { 1, 0, 0, 1, -1, 0, 0, -1 };

		static Location OctantTranslate(Location l, int octant)
		{
			int tx = l.X * xxcomp[octant] + l.Y * xycomp[octant];
			int ty = l.X * yxcomp[octant] + l.Y * yycomp[octant];

			return new Location(tx, ty);
		}
	}
}