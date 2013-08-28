using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dwarrowdelf;
using Dwarrowdelf.TerrainGen;

namespace Dwarrowdelf.TerrainGen
{
	public class DungeonTerrainGenerator
	{
		IntSize3 m_size;
		TerrainData m_data;

		Random m_random;

		Dictionary<int, IntGrid2[]> m_rooms = new Dictionary<int, IntGrid2[]>();

		public Dictionary<int, IntGrid2[]> Rooms { get { return m_rooms; } }

		public DungeonTerrainGenerator(TerrainData data, Random random)
		{
			m_data = data;
			m_size = data.Size;
			m_random = random;
		}

		public void Generate(int seed)
		{
			GenerateTerrain(seed);

			CreateTileGrid();
		}

		void GenerateTerrain(int seed)
		{
			Parallel.For(0, m_size.Height, y =>
				{
					for (int x = 0; x < m_size.Width; ++x)
					{
						m_data.SetSurfaceLevel(x, y, m_size.Depth - 1);
					}
				});
		}

		void CreateTileGrid()
		{
			CreateBaseGrid();

			CreateDungeon();

			CreateStairs();
		}

		void CreateStairs()
		{
			for (int z = m_size.Depth - 1; z > 0; --z)
			{
				var center = new IntPoint2(m_random.Next(m_size.Width), m_random.Next(m_size.Height));

				foreach (var p in IntPoint2.SquareSpiral(center, m_size.Width))
				{
					if (m_size.Plane.Contains(p) == false)
						continue;

					var p1 = new IntPoint3(p, z);
					var td1 = GetTileData(p1);

					if (td1.IsClearFloor == false)
						continue;

					var p2 = new IntPoint3(p, z - 1);
					var td2 = GetTileData(p2);

					if (td2.IsClearFloor == false)
						continue;

					td1.TerrainID = TerrainID.StairsDown;
					td1.TerrainMaterialID = MaterialID.Granite;

					td2.InteriorID = InteriorID.Stairs;
					td2.InteriorMaterialID = MaterialID.Granite;

					SetTileData(p1, td1);
					SetTileData(p2, td2);

					break;
				}
			}
		}

		void CreateBaseGrid()
		{
			int width = m_size.Width;
			int height = m_size.Height;
			int depth = m_size.Depth;

			Parallel.For(0, height, y =>
			{
				for (int x = 0; x < width; ++x)
				{
					int surface = m_data.GetSurfaceLevel(x, y);

					for (int z = 0; z < depth; ++z)
					{
						var p = new IntPoint3(x, y, z);
						var td = new TileData();

						if (z < surface)
						{
							td.TerrainID = TerrainID.NaturalFloor;
							td.TerrainMaterialID = MaterialID.Granite;
							td.InteriorID = InteriorID.NaturalWall;
							td.InteriorMaterialID = MaterialID.Granite;
						}
						else if (z == surface)
						{
							td.TerrainID = TerrainID.NaturalFloor;
							td.TerrainMaterialID = MaterialID.Granite;
							td.InteriorID = InteriorID.Empty;
							td.InteriorMaterialID = MaterialID.Undefined;
						}
						else
						{
							td.TerrainID = TerrainID.Empty;
							td.TerrainMaterialID = MaterialID.Undefined;
							td.InteriorID = InteriorID.Empty;
							td.InteriorMaterialID = MaterialID.Undefined;
						}

						SetTileData(p, td);
					}
				}
			});
		}

		struct BSPNode
		{
			public bool Horiz;
			public IntGrid2 Grid;

			public BSPNode(IntGrid2 grid, bool horiz)
			{
				this.Grid = grid;
				this.Horiz = horiz;
			}

		}

		class BSPTree
		{
			public int Depth { get; private set; }
			public int Length { get { return m_bsp.Length; } }
			BSPNode[] m_bsp;

			public BSPTree(int depth)
			{
				this.Depth = depth;
				m_bsp = new BSPNode[MyMath.Pow2(depth) - 1];
			}

			public BSPNode this[int idx]
			{
				get { return m_bsp[idx]; }
				set { m_bsp[idx] = value; }
			}

			public int GetLeft(int i)
			{
				return 2 * i + 1;
			}

			public int GetRight(int i)
			{
				return 2 * i + 2;
			}

			public int GetParent(int i)
			{
				return (i - 1) / 2;
			}

			public int GetNodeDepth(int i)
			{
				return MyMath.Log2(1 + i);
			}

			public bool IsLeaf(int i)
			{
				return GetNodeDepth(i) + 1 == this.Depth;
			}
		}

		void CreateDungeon()
		{
			int h = MyMath.Log2(m_size.Width) - 0;

			var bsp = new BSPTree(h);

			for (int z = m_size.Depth - 2; z >= 0; --z)
				CreateDungeonLevel(bsp, z);
		}

		void CreateDungeonLevel(BSPTree bsp, int z)
		{
			var root = new IntGrid2(0, 0, m_size.Width, m_size.Height);
			CreateNodes(bsp, root, 0);

			var td = new TileData();
			td.TerrainID = TerrainID.NaturalFloor;
			td.TerrainMaterialID = MaterialID.Granite;
			td.InteriorID = InteriorID.Empty;
			td.InteriorMaterialID = MaterialID.Undefined;

			int leafs = MyMath.Pow2(bsp.Depth - 1);

			var rooms = new List<IntGrid2>();

			// Shrink the full sized leaf nodes for rooms
			for (int l = 0; l < leafs; ++l)
			{
				int i = bsp.Length - l - 1;

				var n = bsp[i];
				var grid = n.Grid;

				var xm = GetRandomDouble(0.2, 0.5);
				var ym = GetRandomDouble(0.2, 0.5);

				int columns = (int)((grid.Columns - 1) * xm);
				int rows = (int)((grid.Rows - 1) * ym);
				int x = m_random.Next(grid.Columns - columns) + grid.X;
				int y = m_random.Next(grid.Rows - rows) + grid.Y;

				n.Grid = new IntGrid2(x, y, columns, rows);

				bsp[i] = n;

				rooms.Add(n.Grid);
			}

			m_rooms[z] = rooms.ToArray();

			for (int l = 0; l < leafs; ++l)
			{
				int i = bsp.Length - l - 1;

				var grid = bsp[i].Grid;

				foreach (var p2 in grid.Range())
				{
					var p = new IntPoint3(p2, z);

					var _td = GetTileData(p);
					if (_td.TerrainID == td.TerrainID)
						Debugger.Break();

					SetTileData(p, td);
				}
			}

			Connect(bsp, 0, true, z);
		}

		void Connect(BSPTree bsp, int i, bool isLeft, int z)
		{
			if (bsp.IsLeaf(i))
				return;

			var left = bsp.GetLeft(i);
			var right = bsp.GetRight(i);

			Connect(bsp, left, true, z);
			Connect(bsp, right, false, z);

			var leftRoom = FindNearestRoom(bsp, left, bsp[right].Grid.Center);
			var rightRoom = FindNearestRoom(bsp, right, bsp[left].Grid.Center);

			ConnectRooms(bsp[leftRoom].Grid, bsp[rightRoom].Grid, z);
		}

		int FindNearestRoom(BSPTree bsp, int i, IntPoint2 p)
		{
			if (bsp.IsLeaf(i))
			{
				return i;
			}
			else
			{
				int left = FindNearestRoom(bsp, bsp.GetLeft(i), p);
				int right = FindNearestRoom(bsp, bsp.GetRight(i), p);

				double dl = (bsp[left].Grid.Center - p).Length;
				double rl = (bsp[right].Grid.Center - p).Length;

				if (dl < rl)
					return left;
				else
					return right;
			}
		}

		void ConnectRooms(IntGrid2 room1, IntGrid2 room2, int z)
		{
			var v = room2.Center - room1.Center;
			bool horiz = Math.Abs(v.X) < Math.Abs(v.Y);

			CreateCorridor(room1.Center, room2.Center, z, horiz);
		}

		void CreateCorridor(IntPoint2 from, IntPoint2 to, int z, bool horiz)
		{
			var td = new TileData();
			td.TerrainID = TerrainID.NaturalFloor;
			td.TerrainMaterialID = MaterialID.Granite;
			td.InteriorID = InteriorID.Empty;
			td.InteriorMaterialID = MaterialID.Undefined;

			if (horiz)
			{
				int yinc = from.Y < to.Y ? 1 : -1;

				int middle = from.Y + (to.Y - from.Y) / 2;

				for (int y = from.Y; y != middle; y += yinc)
					SetTileData(new IntPoint3(from.X, y, z), td);

				int x1 = Math.Min(from.X, to.X);
				int x2 = Math.Max(from.X, to.X);

				for (int x = x1; x <= x2; ++x)
					SetTileData(new IntPoint3(x, middle, z), td);

				for (int y = middle; y != to.Y; y += yinc)
					SetTileData(new IntPoint3(to.X, y, z), td);
			}
			else
			{
				int xinc = from.X < to.X ? 1 : -1;

				int middle = from.X + (to.X - from.X) / 2;

				for (int x = from.X; x != middle; x += xinc)
					SetTileData(new IntPoint3(x, from.Y, z), td);

				int y1 = Math.Min(from.Y, to.Y);
				int y2 = Math.Max(from.Y, to.Y);

				for (int y = y1; y <= y2; ++y)
					SetTileData(new IntPoint3(middle, y, z), td);

				for (int x = middle; x != to.X; x += xinc)
					SetTileData(new IntPoint3(x, to.Y, z), td);
			}
		}

		void CreateNodes(BSPTree bsp, IntGrid2 grid, int i)
		{
			int middle;
			bool horiz;

			if (grid.Columns <= 4 && grid.Rows <= 4)
			{
				Debugger.Break();
				throw new Exception();
			}
			else if (grid.Columns <= 4)
			{
				horiz = true;
			}
			else if (grid.Rows <= 4)
			{
				horiz = false;
			}
			else
			{
				horiz = grid.Columns < grid.Rows;
			}

			double m = GetRandomDouble(0.4, 0.6);

			if (horiz)
				middle = (int)(grid.Rows * m);
			else
				middle = (int)(grid.Columns * m);


			bsp[i] = new BSPNode(grid, horiz);

			if (bsp.IsLeaf(i))
				return;

			int left = bsp.GetLeft(i);
			int right = bsp.GetRight(i);

			if (horiz)
			{
				// up
				var g1 = new IntGrid2(grid.X, grid.Y, grid.Columns, middle);
				CreateNodes(bsp, g1, left);

				// down
				var g2 = new IntGrid2(grid.X, grid.Y + middle + 1, grid.Columns, grid.Rows - middle - 1);
				CreateNodes(bsp, g2, right);
			}
			else
			{
				// left
				var g1 = new IntGrid2(grid.X, grid.Y, middle, grid.Rows);
				CreateNodes(bsp, g1, left);

				// right
				var g2 = new IntGrid2(grid.X + middle + 1, grid.Y, grid.Columns - middle - 1, grid.Rows);
				CreateNodes(bsp, g2, right);
			}
		}

		int GetRandomInt(int max)
		{
			return m_random.Next(max);
		}

		double GetRandomDouble(double min, double max)
		{
			return m_random.NextDouble() * (max - min) + min;
		}

		void SetTileData(IntPoint3 p, TileData td)
		{
			m_data.SetTileData(p, td);
		}

		TileData GetTileData(IntPoint3 p)
		{
			return m_data.GetTileData(p);
		}
	}
}
