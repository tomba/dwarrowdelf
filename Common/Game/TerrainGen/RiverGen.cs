using Dwarrowdelf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.TerrainGen
{
	public class RiverGen
	{
		TerrainData m_terrain;
		Random m_random;

		IntPoint2[] m_riverPath;

		public RiverGen(TerrainData terrain, Random random)
		{
			m_terrain = terrain;
			m_random = random;
		}

		enum SideEdge
		{
			Top,
			Right,
			Bottom,
			Left,
		}

		class MyTarget : IAStarTarget
		{
			TerrainData m_terrain;
			IntPoint3 m_origin;
			SideEdge m_sourceSide;

			public MyTarget(TerrainData terrain, IntPoint3 origin, SideEdge sourceSide)
			{
				m_terrain = terrain;
				m_origin = origin;
				m_sourceSide = sourceSide;
			}

			public bool GetIsTarget(IntPoint3 p)
			{
				if (p.X != 0 && p.Y != 0 && p.X != m_terrain.Width - 1 && p.Y != m_terrain.Height - 1)
					return false;

				return
					(m_sourceSide == SideEdge.Left && p.X > m_terrain.Width / 2) ||
					(m_sourceSide == SideEdge.Top && p.Y > m_terrain.Height / 2) ||
					(m_sourceSide == SideEdge.Right && p.X < m_terrain.Width / 2) ||
					(m_sourceSide == SideEdge.Bottom && p.Y < m_terrain.Height / 2);
			}

			public ushort GetHeuristic(IntPoint3 p)
			{
				// Add a bit random so that the river doesn't go straight
				var r = new MWCRandom(p, 1);
				return (ushort)(p.Z * 10 + r.Next(4));
			}

			public ushort GetCostBetween(IntPoint3 src, IntPoint3 dst)
			{
				return 0;
			}

			public IEnumerable<Direction> GetValidDirs(IntPoint3 p)
			{
				foreach (var d in DirectionExtensions.CardinalUpDownDirections.ToArray())
				{
					var dst = p + d;
					if (m_terrain.Contains(dst) && m_terrain.GetTileData(dst).IsWaterPassable)
						yield return d;
				}
			}
		}

		IntPoint2 MapCoord(int c, SideEdge edge)
		{
			switch (edge)
			{
				case SideEdge.Left:
					return new IntPoint2(0, c);
				case SideEdge.Top:
					return new IntPoint2(c, 0);
				case SideEdge.Right:
					return new IntPoint2(m_terrain.Width - 1, c);
				case SideEdge.Bottom:
					return new IntPoint2(c, m_terrain.Height - 1);
				default:
					throw new Exception();
			}
		}

		IntPoint3 FindStartLoc(Random r, SideEdge edge)
		{
			int side = m_terrain.Width;

			int yo = r.Next(0, side);
			int zo = m_terrain.GetSurfaceLevel(MapCoord(yo, edge));

			int yu = yo;
			int zu = zo;

			for (int y = yo - 1; y >= 0; --y)
			{
				int z = m_terrain.GetSurfaceLevel(MapCoord(y, edge));
				if (z < zu)
				{
					zu = z;
					yu = y;
				}

				if (z - 2 > zu)
					break;
			}

			int yd = yo;
			int zd = zo;

			for (int y = yo + 1; y < side; ++y)
			{
				int z = m_terrain.GetSurfaceLevel(MapCoord(y, edge));
				if (z < zd)
				{
					zd = z;
					yd = y;
				}

				if (z - 2 > zd)
					break;
			}

			int yf = zd < zu ? yd : yu;
			var p2 = MapCoord(yf, edge);
			return m_terrain.GetSurfaceLocation(p2);
		}

		public bool CreateRiverPath()
		{
			int offset = m_random.Next();

			m_riverPath = null;

			for (int i = 0; i < 8; ++i)
			{
				SideEdge edge = (SideEdge)((i + offset) % 4);

				var startLoc = FindStartLoc(m_random, edge);

				var target = new MyTarget(m_terrain, startLoc, edge);
				int maxNodeCount = 500000; // XXX this should reflect the map size
				var res = AStar.Find(new IntPoint3[] { startLoc }, target, maxNodeCount);

				if (res.Status != AStarStatus.Found)
					continue;

				var riverPath = res.GetPathLocationsReverse().Select(p => p.ToIntPoint2()).ToArray();

				if (riverPath.Length < 100)
				{
					Trace.TraceInformation("retry, too short");
					continue;
				}

				int tot = riverPath.Aggregate(0, (a, p) =>
					a + MyMath.Min(p.X, p.Y, m_terrain.Width - p.X - 1, m_terrain.Height - p.Y - 1));

				int avg = tot / riverPath.Length;

				if (avg < 20)
				{
					Trace.TraceInformation("too close to edge, avg {0}", avg);
					continue;
				}

				m_riverPath = riverPath;

				return true;
			}

			return false;
		}

		public void AdjustRiver()
		{
			int minZ = m_riverPath.Min(p => m_terrain.GetSurfaceLevel(p));

			var pos = DirectionSet.Cardinal | DirectionSet.Exact;

			var coreLocs = new HashSet<IntPoint2>(m_riverPath.SelectMany(p => pos.ToSurroundingPoints(p)));

			foreach (var pp in coreLocs)
			{
				if (m_terrain.Size.Plane.Contains(pp) == false)
					continue;

				for (int z = m_terrain.Depth - 1; z >= minZ - 1; --z)
				{
					var p = new IntPoint3(pp.X, pp.Y, z);

					var td = m_terrain.GetTileData(p);
					td.InteriorID = InteriorID.Empty;
					td.InteriorMaterialID = MaterialID.Undefined;

					if (z == minZ - 1)
					{
						td.TerrainID = TerrainID.NaturalFloor;
						td.WaterLevel = TileData.MaxWaterLevel;
					}
					else
					{
						td.TerrainID = TerrainID.Empty;
						td.TerrainMaterialID = MaterialID.Undefined;
					}

					m_terrain.SetTileData(p, td);
				}
			}
		}
	}
}
