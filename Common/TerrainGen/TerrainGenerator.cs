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
	public class TerrainGenerator
	{
		ArrayGrid2D<byte> m_heightMap;

		IntSize3 m_size;

		public ArrayGrid2D<byte> HeightMap { get { return m_heightMap; } }
		public TileGrid TileGrid { get; private set; }

		Tuple<double, double> m_rockLayerSlant;

		Random m_random;

		public TerrainGenerator(IntSize3 size, Random random)
		{
			m_size = size;
			m_random = random;
		}

		public void Generate(DiamondSquare.CornerData corners, double range, double h, int seed, double amplify)
		{
			m_heightMap = GenerateTerrain(corners, range, h, seed, amplify);

			CreateTileGrid();
		}

		ArrayGrid2D<byte> GenerateTerrain(DiamondSquare.CornerData corners, double range, double h,
			int seed, double amplify)
		{
			// +1 for diamond square
			var doubleHeightMap = new ArrayGrid2D<double>(m_size.Width + 1, m_size.Height + 1);

			double min, max;

			DiamondSquare.Render(doubleHeightMap, corners, range, h, seed, out min, out max);

			var heightMap = new ArrayGrid2D<byte>(m_size.Width, m_size.Height);

			Parallel.For(0, m_size.Height, y =>
				{
					double d = max - min;

					for (int x = 0; x < m_size.Width; ++x)
					{
						var v = doubleHeightMap[x, y];

						// normalize to 0.0 - 1.0
						v = (v - min) / d;

						// amplify
						v = Math.Pow(v, amplify);

						// adjust
						v *= m_size.Depth / 2;
						v += m_size.Depth / 2 - 1;

						heightMap[x, y] = (byte)Math.Round(v);
					}
				});

			return heightMap;
		}

		void CreateTileGrid()
		{
			this.TileGrid = new TileGrid(m_size);

			CreateBaseGrid();

			CreateOreVeins();

			CreateOreClusters();

			CreateSoil(this.TileGrid, this.HeightMap);

			CreateSlopes(this.TileGrid, this.HeightMap, m_random.Next());
		}

		void CreateBaseGrid()
		{
			int width = m_size.Width;
			int height = m_size.Height;
			int depth = m_size.Depth;

			var rockMaterials = Materials.GetMaterials(MaterialCategory.Rock).ToArray();
			var layers = new MaterialID[20];

			{
				int rep = 0;
				MaterialID mat = MaterialID.Undefined;
				for (int z = 0; z < layers.Length; ++z)
				{
					if (rep == 0)
					{
						rep = m_random.Next(4) + 1;
						mat = rockMaterials[m_random.Next(rockMaterials.Length - 1)].ID;
					}

					layers[z] = mat;
					rep--;
				}
			}

			double xk = (GetRandomDouble() * 2 - 1) * 0.01;
			double yk = (GetRandomDouble() * 2 - 1) * 0.01;

			m_rockLayerSlant = new Tuple<double, double>(xk, yk);

			Parallel.For(0, height, y =>
			{
				for (int x = 0; x < width; ++x)
				{
					int surface = m_heightMap[x, y];

					for (int z = 0; z < depth; ++z)
					{
						var p = new IntPoint3(x, y, z);
						var td = new TileData();

						if (z < surface)
						{
							td.TerrainID = TerrainID.NaturalWall;

							int _z = (int)Math.Round(z + x * xk + y * yk);

							_z = _z % layers.Length;

							if (_z < 0)
								_z += layers.Length;

							td.TerrainMaterialID = layers[_z];
						}
						else if (z == surface)
						{
							td.TerrainID = TerrainID.NaturalFloor;
							td.TerrainMaterialID = GetTileData(new IntPoint3(x, y, z - 1)).TerrainMaterialID;
						}
						else
						{
							td.TerrainID = TerrainID.Empty;
							td.TerrainMaterialID = MaterialID.Undefined;
						}

						td.InteriorID = InteriorID.Empty;
						td.InteriorMaterialID = MaterialID.Undefined;

						SetTileData(p, td);
					}
				}
			});
		}

		static void CreateSoil(TileGrid grid, ArrayGrid2D<byte> heightMap)
		{
			int soilLimit = grid.Depth * 4 / 5;

			int w = grid.Width;
			int h = grid.Height;

			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					int z = heightMap[x, y];

					var p = new IntPoint3(x, y, z);

					if (z < soilLimit)
					{
						var td = grid.GetTileData(p);

						td.TerrainMaterialID = MaterialID.Loam;

						grid.SetTileData(p, td);
					}
				}
			}
		}

		static void CreateSlopes(TileGrid grid, ArrayGrid2D<byte> heightMap, int baseSeed)
		{
			var arr = new System.Threading.ThreadLocal<Direction[]>(() => new Direction[8]);

			var plane = grid.Size.Plane;

			plane.Range().AsParallel().ForAll(p =>
			{
				int z = heightMap[p];

				int count = 0;
				Direction dir = Direction.None;

				var r = new MWCRandom(p, baseSeed);

				int offset = r.Next(8);

				// Count the tiles around this tile which are higher. Create slope to a random direction, but skip
				// the slope if all 8 tiles are higher.
				// Count to 10. If 3 successive slopes, create one in the middle
				int successive = 0;
				for (int i = 0; i < 10; ++i)
				{
					var d = DirectionExtensions.PlanarDirections[(i + offset) % 8];

					var t = p + d;

					if (plane.Contains(t) && heightMap[t] > z)
					{
						if (i < 8)
							count++;
						successive++;

						if (successive == 3)
						{
							dir = DirectionExtensions.PlanarDirections[((i - 1) + offset) % 8];
						}
						else if (dir == Direction.None)
						{
							dir = d;
						}
					}
					else
					{
						successive = 0;
					}
				}

				if (count > 0 && count < 8)
				{
					var p3d = new IntPoint3(p, z);

					var td = grid.GetTileData(p3d);
					td.TerrainID = dir.ToSlope();
					grid.SetTileData(p3d, td);
				}
			});
		}

		void CreateOreClusters()
		{
			var clusterMaterials = Materials.GetMaterials(MaterialCategory.Gem).Select(mi => mi.ID).ToArray();
			for (int i = 0; i < 100; ++i)
			{
				var p = GetRandomSubterraneanLocation();
				CreateOreCluster(p, clusterMaterials[GetRandomInt(clusterMaterials.Length)]);
			}
		}

		void CreateOreVeins()
		{
			double xk = m_rockLayerSlant.Item1;
			double yk = m_rockLayerSlant.Item2;

			var veinMaterials = Materials.GetMaterials(MaterialCategory.Mineral).Select(mi => mi.ID).ToArray();

			for (int i = 0; i < 100; ++i)
			{
				var start = GetRandomSubterraneanLocation();
				var mat = veinMaterials[GetRandomInt(veinMaterials.Length)];
				int len = GetRandomInt(20) + 3;
				int thickness = GetRandomInt(4) + 1;

				var vx = GetRandomDouble() * 2 - 1;
				var vy = GetRandomDouble() * 2 - 1;
				var vz = vx * xk + vy * yk;

				var v = new DoubleVector3(vx, vy, -vz).Normalize();

				for (double t = 0.0; t < len; t += 1)
				{
					var p = start + (v * t).ToIntVector3();

					CreateOreSphere(p, thickness, mat, GetRandomDouble() * 0.75, 0);
				}
			}
		}

		void CreateOreSphere(IntPoint3 center, int r, MaterialID oreMaterialID, double probIn, double probOut)
		{
			// adjust r, so that r == 1 gives sphere of one tile
			r -= 1;

			// XXX split the sphere into 8 parts, and mirror

			var bb = new IntBox(center.X - r, center.Y - r, center.Z - r, r * 2 + 1, r * 2 + 1, r * 2 + 1);

			var rs = Math.Pow(r, 2);

			foreach (var p in bb.Range())
			{
				var y = p.Y;
				var x = p.X;
				var z = p.Z;

				var v = Math.Pow((x - center.X), 2) + Math.Pow((y - center.Y), 2) + Math.Pow((z - center.Z), 2);

				if (rs >= v)
				{
					var rr = Math.Sqrt(v);

					double rel;

					if (r == 0)
						rel = 1;
					else
						rel = 1 - rr / r;

					var prob = (probIn - probOut) * rel + probOut;

					if (GetRandomDouble() <= prob)
						CreateOre(p, oreMaterialID);
				}
			}
		}

		void CreateOre(IntPoint3 p, MaterialID oreMaterialID)
		{
			if (!m_size.Contains(p))
				return;

			var td = GetTileData(p);

			if (td.TerrainID != TerrainID.NaturalWall)
				return;

			td.InteriorID = InteriorID.Ore;
			td.InteriorMaterialID = oreMaterialID;
			SetTileData(p, td);
		}

		int GetRandomInt(int max)
		{
			return m_random.Next(max);
		}

		double GetRandomDouble()
		{
			return m_random.NextDouble();
		}

		void SetTileData(IntPoint3 p, TileData td)
		{
			this.TileGrid.SetTileData(p, td);
		}

		TileData GetTileData(IntPoint3 p)
		{
			return this.TileGrid.GetTileData(p);
		}

		void CreateOreCluster(IntPoint3 p, MaterialID oreMaterialID)
		{
			CreateOreCluster(p, oreMaterialID, GetRandomInt(6) + 1);
		}

		void CreateOreCluster(IntPoint3 p, MaterialID oreMaterialID, int count)
		{
			if (!m_size.Contains(p))
				return;

			var td = GetTileData(p);

			if (td.TerrainID != TerrainID.NaturalWall)
				return;

			if (td.InteriorID == InteriorID.Ore)
				return;

			td.InteriorID = InteriorID.Ore;
			td.InteriorMaterialID = oreMaterialID;
			SetTileData(p, td);

			if (count > 0)
			{
				foreach (var d in DirectionExtensions.CardinalUpDownDirections)
					CreateOreCluster(p + d, oreMaterialID, count - 1);
			}
		}

		IntPoint3 GetRandomSubterraneanLocation()
		{
			int x = GetRandomInt(m_size.Width);
			int y = GetRandomInt(m_size.Height);
			int maxZ = m_heightMap[x, y];
			int z = GetRandomInt(maxZ);

			return new IntPoint3(x, y, z);
		}
	}
}
