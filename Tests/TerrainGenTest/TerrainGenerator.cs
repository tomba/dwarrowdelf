using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dwarrowdelf;
using Dwarrowdelf.TerrainGen;

namespace TerrainGenTest
{
	public class TerrainGenerator
	{
		TileData[, ,] m_grid;
		ArrayGrid2D<double> m_doubleHeightMap;
		ArrayGrid2D<int> m_heightMap;

		IntSize3 m_size;

		public ArrayGrid2D<int> HeightMap { get { return m_heightMap; } }
		public TileData[, ,] TileGrid { get { return m_grid; } }

		public double Average { get; private set; }

		Random m_random = new Random(1);

		public TerrainGenerator(IntSize3 size)
		{
			m_size = size;

			int w = size.Width;
			int h = size.Height;
			int d = size.Depth;

			m_doubleHeightMap = new ArrayGrid2D<double>(w, h);
			m_heightMap = new ArrayGrid2D<int>(w, h);
			m_grid = new TileData[d, h, w];
		}

		public void Generate(DiamondSquare.CornerData corners, double range, double h, int seed, double amplify)
		{
			m_doubleHeightMap.Clear();

			GenerateTerrain(m_doubleHeightMap, corners, range, h, seed, amplify);

			AnalyzeTerrain(m_doubleHeightMap);

			// integer heightmap. the number tells the z level where the floor is.
			foreach (var p in IntPoint2.Range(m_size.Width, m_size.Height))
			{
				var d = m_doubleHeightMap[p];

				d *= m_size.Depth / 2;
				d += (m_size.Depth / 2) - 1;

				m_heightMap[p] = (int)Math.Round(d);
			}

			CreateTileGrid();
		}

		int GetRandomInt(int max)
		{
			return m_random.Next(max);
		}

		double GetRandomDouble()
		{
			return m_random.NextDouble();
		}

		void GenerateTerrain(ArrayGrid2D<double> grid, DiamondSquare.CornerData corners, double range, double h,
			int seed, double amplify)
		{
			DiamondSquare.Render(grid, corners, range, h, seed);

			//Clamper.Clamp(grid, 10);

			Clamper.Normalize(grid);

			grid.ForEach(v => Math.Pow(v, amplify));

			//Clamper.Normalize(grid);
		}

		void AnalyzeTerrain(ArrayGrid2D<double> grid)
		{
			this.Average = grid.Average();
		}

		void CreateTileGrid()
		{
			int width = m_doubleHeightMap.Width;
			int height = m_doubleHeightMap.Height;
			int depth = m_size.Depth;

			var rockMaterials = Materials.GetMaterials(MaterialCategory.Rock).ToArray();
			var layers = new MaterialID[20];

			var random = new Random();

			{
				int rep = 0;
				MaterialID mat = MaterialID.Undefined;
				for (int z = 0; z < layers.Length; ++z)
				{
					if (rep == 0)
					{
						rep = random.Next(4) + 1;
						mat = rockMaterials[random.Next(rockMaterials.Length - 1)].ID;
					}

					layers[z] = mat;
					rep--;
				}
			}

			double xk = (GetRandomDouble() * 2 - 1) * 0.01;
			double yk = (GetRandomDouble() * 2 - 1) * 0.01;

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

			var clusterMaterials = Materials.GetMaterials(MaterialCategory.Gem).Select(mi => mi.ID).ToArray();
			for (int i = 0; i < 100; ++i)
			{
				var p = GetRandomSubterraneanLocation();
				CreateOreCluster(p, clusterMaterials[GetRandomInt(clusterMaterials.Length)]);
			}
		}

		void CreateOreSphere(IntPoint3 center, int r, MaterialID oreMaterialID, double probIn, double probOut)
		{
			// adjust r, so that r == 1 gives sphere of one tile
			r -= 1;

			// XXX split the sphere into 8 parts, and mirror

			var bb = new IntCuboid(center.X - r, center.Y - r, center.Z - r, r * 2 + 1, r * 2 + 1, r * 2 + 1);

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

		void SetTileData(IntPoint3 p, TileData td)
		{
			m_grid[p.Z, p.Y, p.X] = td;
		}

		TileData GetTileData(IntPoint3 p)
		{
			return m_grid[p.Z, p.Y, p.X];
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
