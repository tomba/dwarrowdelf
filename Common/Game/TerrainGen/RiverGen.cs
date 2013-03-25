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
		Random m_random;

		IntPoint2[] m_riverPath;

		Dictionary<IntPoint3, AStarNode> m_astarNodes;
		HashSet<IntPoint3> m_pathPoints;

		TerrainData m_terrain;

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
			Direction[] m_dirs;
			MWCRandom m_r;
			SideEdge m_sourceSide;

			public MyTarget(TerrainData terrain, IntPoint3 origin, SideEdge sourceSide)
			{
				m_dirs = DirectionExtensions.CardinalUpDownDirections.ToArray();

				m_terrain = terrain;
				m_origin = origin;
				m_r = new MWCRandom(m_origin, 1);
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
				return (ushort)(p.Z * 10 + m_r.Next(4));
			}

			public ushort GetCostBetween(IntPoint3 src, IntPoint3 dst)
			{
				return 0;
			}

			public IEnumerable<Direction> GetValidDirs(IntPoint3 p)
			{
				int offset = m_r.Next();

				for (int i = 0; i < m_dirs.Length; ++i)
				{
					int idx = (offset + i) % m_dirs.Length;
					var d = m_dirs[idx];

					var dst = p + d;
					if (m_terrain.Contains(dst) && m_terrain.GetTileData(p + d).IsWaterPassable)
						yield return d;
				}
			}
		}

		IntPoint2 MapCoord(int c, SideEdge quadrant)
		{
			switch (quadrant)
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

		IntPoint3 FindStartLoc(Random r, SideEdge quadrant)
		{
			int side = m_terrain.Width;

			int yo = r.Next(0, side);
			int zo = m_terrain.GetHeight(MapCoord(yo, quadrant));

			int yu = yo;
			int zu = zo;

			for (int y = yo - 1; y >= 0; --y)
			{
				int z = m_terrain.GetHeight(MapCoord(y, quadrant));
				if (z < zu)
				{
					zu = z;
					yu = y;
				}

				if (z - 10 > zu)
					break;
			}

			int yd = yo;
			int zd = zo;

			for (int y = yo + 1; y < side; ++y)
			{
				int z = m_terrain.GetHeight(MapCoord(y, quadrant));
				if (z < zd)
				{
					zd = z;
					yd = y;
				}

				if (z - 10 > zd)
					break;
			}

			int yf = zd < zu ? yd : yu;
			var p2 = MapCoord(yf, quadrant);
			return new IntPoint3(p2, m_terrain.GetHeight(p2));
		}

		public void CreateRiverPath()
		{
			while (true)
			{
				SideEdge q = (SideEdge)m_random.Next(4);

				var startLoc = FindStartLoc(m_random, q);

				var target = new MyTarget(m_terrain, startLoc, q);
				var astar = new AStar(new IntPoint3[] { startLoc }, target);
				astar.MaxNodeCount = 500000;

				var res = astar.Find();

				if (res == AStarStatus.Found)
				{
					Trace.TraceInformation("found route, {0} nodes", astar.Nodes.Count);

					m_riverPath = astar.GetPathNodesReverse().Select(n => n.Loc.ToIntPoint()).ToArray();

					if (m_riverPath.Length < 100)
					{
						Trace.TraceInformation("retry");
						m_riverPath = null;
						continue;
					}

					break;
				}
				else
				{
					break;
				}
			}
		}

		public void AdjustRiver()
		{
			int minZ = m_riverPath.Min(p => m_terrain.GetHeight(p));

			var pos = DirectionSet.Cardinal | DirectionSet.Exact;

			var coreLocs = new HashSet<IntPoint2>(m_riverPath.SelectMany(p => pos.ToSurroundingPoints(p)));
			var sideLocs = new HashSet<IntPoint2>(coreLocs.SelectMany(p => pos.ToSurroundingPoints(p)).Except(coreLocs));

			foreach (var pp in coreLocs)
			{
				if (m_terrain.Size.Plane.Contains(pp) == false)
					continue;

				for (int z = m_terrain.Depth - 1; z >= minZ - 2; --z)
				{
					var p = new IntPoint3(pp.X, pp.Y, z);

					var td = m_terrain.GetTileData(p);
					td.InteriorID = InteriorID.Empty;
					td.InteriorMaterialID = MaterialID.Undefined;

					if (z == minZ - 2)
					{
						td.TerrainID = TerrainID.NaturalFloor;
						td.WaterLevel = TileData.MaxWaterLevel;
					}
					else if (z == minZ - 1)
					{
						td.WaterLevel = TileData.MaxWaterLevel;
						td.TerrainID = TerrainID.Empty;
						td.TerrainMaterialID = MaterialID.Undefined;
					}
					else
					{
						td.TerrainID = TerrainID.Empty;
						td.TerrainMaterialID = MaterialID.Undefined;
					}

					m_terrain.SetTileData(p, td);
				}
			}

			foreach (var pp in sideLocs)
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
