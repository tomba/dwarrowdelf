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
		IntSize3 m_size;
		TerrainData m_data;

		Tuple<double, double> m_rockLayerSlant;

		Random m_random;

		public TerrainGenerator(TerrainData data, Random random)
		{
			m_data = data;
			m_size = data.Size;
			m_random = random;
		}

		public void Generate(DiamondSquare.CornerData corners, double range, double h, double amplify)
		{
			GenerateTerrain(corners, range, h, amplify);

			CreateTileGrid();
		}

		void GenerateTerrain(DiamondSquare.CornerData corners, double range, double h, double amplify)
		{
			// +1 for diamond square
			var heightMap = new ArrayGrid2D<double>(m_size.Width + 1, m_size.Height + 1);

			double min, max;

			DiamondSquare.Render(heightMap, corners, range, h, m_random, out min, out max);

			var levelMap = m_data.LevelMap;

			Parallel.For(0, m_size.Height, y =>
				{
					double d = max - min;

					for (int x = 0; x < m_size.Width; ++x)
					{
						var v = heightMap[x, y];

						// normalize to 0.0 - 1.0
						v = (v - min) / d;

						// amplify
						v = Math.Pow(v, amplify);

						// adjust
						v *= m_size.Depth / 2;
						v += m_size.Depth / 2 - 1;

						levelMap[y, x] = (byte)Math.Round(v);
					}
				});
		}

		void CreateTileGrid()
		{
			CreateBaseGrid();

			CreateOreVeins();

			CreateOreClusters();

			var riverGen = new RiverGen(m_data, m_random);
			if (riverGen.CreateRiverPath())
			{
				riverGen.AdjustRiver();
			}
			else
			{
				Trace.TraceError("Failed to create river");
			}

			int soilLimit = m_size.Depth * 4 / 5;
			TerrainHelpers.CreateSoil(m_data, soilLimit);

			TerrainHelpers.CreateSlopes(m_data, m_random.Next());
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
					int surface = m_data.GetSurfaceLevel(x, y);

					for (int z = 0; z < depth; ++z)
					{
						var p = new IntPoint3(x, y, z);
						var td = new TileData();

						if (z < surface)
						{
							td.TerrainID = TerrainID.NaturalFloor;
							td.InteriorID = InteriorID.NaturalWall;

							int _z = (int)Math.Round(z + x * xk + y * yk);

							_z = _z % layers.Length;

							if (_z < 0)
								_z += layers.Length;

							td.TerrainMaterialID = td.InteriorMaterialID = layers[_z];

							SetTileDataNoHeight(p, td);
						}
						else if (z == surface)
						{
							td.TerrainID = TerrainID.NaturalFloor;
							td.TerrainMaterialID = GetTileData(new IntPoint3(x, y, z - 1)).TerrainMaterialID;
							td.InteriorID = InteriorID.Empty;
							td.InteriorMaterialID = MaterialID.Undefined;
							SetTileData(p, td);
						}
						else
						{
							td.TerrainID = TerrainID.Empty;
							td.TerrainMaterialID = MaterialID.Undefined;
							td.InteriorID = InteriorID.Empty;
							td.InteriorMaterialID = MaterialID.Undefined;
							SetTileDataNoHeight(p, td);
						}
					}
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

			var bb = new IntGrid3(center.X - r, center.Y - r, center.Z - r, r * 2 + 1, r * 2 + 1, r * 2 + 1);

			var rs = MyMath.Square(r);

			foreach (var p in bb.Range())
			{
				var y = p.Y;
				var x = p.X;
				var z = p.Z;

				var v = MyMath.Square(x - center.X) + MyMath.Square(y - center.Y) + MyMath.Square(z - center.Z);

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

			if (td.InteriorID != InteriorID.NaturalWall)
				return;

			td.InteriorMaterialID = oreMaterialID;
			SetTileDataNoHeight(p, td);
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
			m_data.SetTileData(p, td);
		}

		void SetTileDataNoHeight(IntPoint3 p, TileData td)
		{
			m_data.SetTileDataNoHeight(p, td);
		}

		TileData GetTileData(IntPoint3 p)
		{
			return m_data.GetTileData(p);
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

			if (td.InteriorID != InteriorID.NaturalWall)
				return;

			if (Materials.GetMaterial(td.InteriorMaterialID).Category != MaterialCategory.Rock)
				return;

			td.InteriorMaterialID = oreMaterialID;
			SetTileDataNoHeight(p, td);

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
			int maxZ = m_data.GetSurfaceLevel(x, y);
			int z = GetRandomInt(maxZ);

			return new IntPoint3(x, y, z);
		}
	}
}
