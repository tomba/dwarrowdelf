using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public interface ILOSAlgo
	{
		void Calculate(IntPoint2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap,
			IntRect mapBounds, Func<IntPoint2, bool> blockerDelegate);
	}

	public sealed class LOSNull : ILOSAlgo
	{
		public void Calculate(IntPoint2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap,
			IntRect mapBounds, Func<IntPoint2, bool> blockerDelegate)
		{
			for (int y = -visionRange; y <= visionRange; ++y)
			{
				for (int x = -visionRange; x <= visionRange; ++x)
				{
					var l = new IntPoint2(x, y);
					if (mapBounds.Contains(l + (IntVector2)viewerLocation))
						visibilityMap[l] = true;
					else
						visibilityMap[l] = true;
				}
			}
		}
	}

	// http://sc.tri-bit.com/Computing_LOS_for_Large_Areas
	public sealed class LOSShadowCast1 : ILOSAlgo
	{
		sealed class LOSCell // xxx struct?
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

		LOSCell[] m_cells;
		static bool s_tested;
		
		// blockerDelegate(location) returns if tile at location is a blocker. slow?
		public void Calculate(IntPoint2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap, 
			IntRect mapBounds, Func<IntPoint2, bool> blockerDelegate)
		{
			if (s_tested == false)
			{
				s_tested = true;
				Test();
			}

			visionRange += 1; // visionrange does not include the tile where the observer is

			if (m_cells == null || m_cells.Length < visionRange)
			{
				m_cells = new LOSCell[visionRange];
				for (int i = 0; i < visionRange; i++)
					m_cells[i] = new LOSCell();
			}

			for (int i = 0; i < 8; i++)
				CalculateLOS(viewerLocation, visionRange, visibilityMap, mapBounds, blockerDelegate, i);
		}

		void CalculateLOS(IntPoint2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap,
			IntRect mapBounds, Func<IntPoint2, bool> blockerDelegate, int octant)
		{
			// Cell (0,0) is assumed to be lit and visible in all cases.
			m_cells[0].Initialize();

			//bool visibleCorner = false;

			for (int x = 0; x < visionRange; x++)
			{
				for (int y = 0; y <= x; y++)
				{
					IntPoint2 translatedLocation = OctantTranslate(new IntPoint2(x, y), octant);
					IntPoint2 mapLocation = translatedLocation + (IntVector2)viewerLocation;

					LOSCell cell = m_cells[y];
					LOSCell cellS = null;
					if (y > 0)
						cellS = m_cells[y - 1];

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

					// this makes the LOS area round
					if (new IntVector2(x, y).Length > visionRange)
					{
						// I'm sure this could be integrated better in the code above.
						// perhaps this could even be in the beginning of the loop
						visibilityMap[translatedLocation] = false;
					}
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

		static IntPoint2 OctantTranslate(IntPoint2 l, int octant)
		{
			int tx = l.X * xxcomp[octant] + l.Y * xycomp[octant];
			int ty = l.X * yxcomp[octant] + l.Y * yycomp[octant];

			return new IntPoint2(tx, ty);
		}

		static void Test()
		{
			const int w = 7;

			int[,] blocks = {
							 { 0, 0, 0, 0, 0, 0, 0 },
							 { 0, 0, 1, 1, 1, 0, 0 },
							 { 0, 0, 0, 0, 0, 0, 0 },
							 { 0, 1, 0, 0, 1, 0, 0 },
							 { 0, 0, 0, 0, 0, 0, 0 },
							 { 0, 0, 0, 0, 0, 0, 0 },
							 { 0, 0, 1, 1, 1, 0, 0 },
							};

			int[,] expected = {
							 { 0, 1, 1, 1, 1, 1, 0 },
							 { 0, 0, 0, 0, 0, 0, 1 },
							 { 0, 0, 0, 0, 0, 1, 1 },
							 { 1, 0, 0, 0, 0, 1, 1 },
							 { 0, 0, 0, 0, 0, 1, 1 },
							 { 0, 0, 0, 0, 0, 0, 1 },
							 { 0, 0, 0, 0, 0, 0, 0 },
							};

			Grid2D<bool> vis = new Grid2D<bool>(w, w, w/2, w/2);
			IntPoint2 loc = new IntPoint2(w/2, w/2);
			IntRect bounds = new IntRect(0, 0, w, w);
			LOSShadowCast1 los = new LOSShadowCast1();

			los.Calculate(loc, 3, vis, bounds, l => blocks[l.Y, l.X] != 0);
			vis.Origin = new IntVector2(0, 0);
			for (int y = 0; y < w; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					if (vis[x, y] && expected[y, x] != 0)
						throw new Exception("LOS algo failed self check");
				}
			}
		}

		public static long PerfTest()
		{
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			DoPerfTest();
			sw.Stop();
			return sw.ElapsedTicks;
		}

		static void DoPerfTest()
		{
			const int w = 13;
			Random rand = new Random(1234);

			bool[,] blocks = new bool[w, w];

			for (int y = 0; y < w; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					blocks[x, y] = rand.Next() % 2 == 0;
				}
			}

			Grid2D<bool> vis = new Grid2D<bool>(w, w, w / 2, w / 2);
			IntPoint2 loc = new IntPoint2(w / 2, w / 2);
			IntRect bounds = new IntRect(0, 0, w, w);
			LOSShadowCast1 los = new LOSShadowCast1();

			// 1.4M ticks
			for (int i = 0; i < 5000; ++i)
				los.Calculate(loc, w / 2, vis, bounds, l => blocks[l.Y, l.X]);
		}
	}
}