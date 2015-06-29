using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;

using Dwarrowdelf.TerrainGen;
using System.Threading.Tasks;
using System.Threading;

namespace Dwarrowdelf.TerrainGen
{
	public static class TerrainHelpers
	{
		public static void CreateBaseMinerals(TerrainData terrain, Random random, double xk, double yk)
		{
			int width = terrain.Width;
			int height = terrain.Height;
			int depth = terrain.Depth;

			var rockMaterials = Materials.GetMaterials(MaterialCategory.Rock).ToArray();
			var layers = new MaterialID[20];

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

			Parallel.For(0, height, y =>
			{
				for (int x = 0; x < width; ++x)
				{
					int surface = terrain.GetSurfaceLevel(x, y);

					for (int z = 0; z < surface; ++z)
					{
						var p = new IntVector3(x, y, z);

						int _z = MyMath.Round(z + x * xk + y * yk);

						_z = _z % layers.Length;

						if (_z < 0)
							_z += layers.Length;

						terrain.SetTileDataNoHeight(p, TileData.GetNaturalWall(layers[_z]));
					}
				}
			});
		}

		public static void CreateOreVeins(TerrainData terrain, Random random, double xk, double yk)
		{
			var veinMaterials = Materials.GetMaterials(MaterialCategory.Mineral).Select(mi => mi.ID).ToArray();

			for (int i = 0; i < 100; ++i)
			{
				var start = GetRandomSubterraneanLocation(terrain, random);
				var mat = veinMaterials[random.Next(veinMaterials.Length)];
				int len = random.Next(20) + 3;
				int thickness = random.Next(4) + 1;

				var vx = random.NextDouble() * 2 - 1;
				var vy = random.NextDouble() * 2 - 1;
				var vz = vx * xk + vy * yk;

				var v = new DoubleVector3(vx, vy, -vz).Normalize();

				for (double t = 0.0; t < len; t += 1)
				{
					var p = start + (v * t).ToIntVector3();

					CreateOreSphere(terrain, random, p, thickness, mat, random.NextDouble() * 0.75, 0);
				}
			}
		}

		static void CreateOreSphere(TerrainData terrain, Random random, IntVector3 center, int r, MaterialID oreMaterialID, double probIn, double probOut)
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

					if (random.NextDouble() <= prob)
						CreateOre(terrain, p, oreMaterialID);
				}
			}
		}

		public static void CreateOreClusters(TerrainData terrain, Random random)
		{
			var clusterMaterials = Materials.GetMaterials(MaterialCategory.Gem).Select(mi => mi.ID).ToArray();
			for (int i = 0; i < 100; ++i)
			{
				var p = GetRandomSubterraneanLocation(terrain, random);
				CreateOreCluster(terrain, random, p, clusterMaterials[random.Next(clusterMaterials.Length)]);
			}
		}

		static void CreateOreCluster(TerrainData terrain, Random random, IntVector3 p, MaterialID oreMaterialID)
		{
			CreateOreCluster(terrain, p, oreMaterialID, random.Next(6) + 1);
		}

		static void CreateOreCluster(TerrainData terrain, IntVector3 p, MaterialID oreMaterialID, int count)
		{
			bool b = CreateOre(terrain, p, oreMaterialID);
			if (b == false)
				return;

			if (count > 0)
			{
				foreach (var d in DirectionExtensions.CardinalUpDownDirections)
					CreateOreCluster(terrain, p + d, oreMaterialID, count - 1);
			}
		}

		static bool CreateOre(TerrainData terrain, IntVector3 p, MaterialID oreMaterialID)
		{
			if (!terrain.Contains(p))
				return false;

			var td = terrain.GetTileData(p);

			if (td.ID != TileID.NaturalWall)
				return false;

			if (Materials.GetMaterial(td.MaterialID).Category != MaterialCategory.Rock)
				return false;

			td.SecondaryMaterialID = oreMaterialID; // ZZZ
			terrain.SetTileDataNoHeight(p, td);

			return true;
		}

		static IntVector3 GetRandomSubterraneanLocation(TerrainData data, Random random)
		{
			int x = random.Next(data.Width);
			int y = random.Next(data.Height);
			int maxZ = data.GetSurfaceLevel(x, y);
			int z = random.Next(maxZ);

			return new IntVector3(x, y, z);
		}

		public static void CreateSoil(TerrainData data, int soilLimit)
		{
			int w = data.Width;
			int h = data.Height;

			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					int z = data.GetSurfaceLevel(x, y);

					if (z < soilLimit)
					{
						var p = new IntVector3(x, y, z - 1);

						data.SetTileDataNoHeight(p, TileData.GetNaturalWall(MaterialID.Loam));
					}
				}
			}
		}

		public static void CreateVegetation(TerrainData terrain, Random random, int vegetationLimit)
		{
			var grassMaterials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();
			var woodMaterials = Materials.GetMaterials(MaterialCategory.Wood).ToArray();
			var berryMaterials = Materials.GetMaterials(MaterialCategory.Berry).ToArray();

			int baseSeed = random.Next();
			if (baseSeed == 0)
				baseSeed = 1;

			terrain.Size.Plane.Range().AsParallel().ForAll(p2d =>
			{
				int z = terrain.GetSurfaceLevel(p2d);

				var p = new IntVector3(p2d, z);

				if (z >= vegetationLimit)
					return;

				var td = terrain.GetTileData(p);

				if (td.WaterLevel > 0)
					return;

				if (terrain.GetMaterial(p.Down).Category != MaterialCategory.Soil)
					return;

				var r = new MWCRandom(p, baseSeed);

				int v = r.Next(100);

				if (v >= 95)
				{
					td.ID = TileID.Sapling;
					td.MaterialID = woodMaterials[r.Next(woodMaterials.Length)].ID;
				}
				else if (v >= 90)
				{
					td.ID = TileID.Tree;
					td.MaterialID = woodMaterials[r.Next(woodMaterials.Length)].ID;
				}
				else if (v >= 80)
				{
					td.ID = TileID.Shrub;
					td.MaterialID = berryMaterials[r.Next(berryMaterials.Length)].ID;
				}
				else
				{
					td.ID = TileID.Grass;
					td.MaterialID = grassMaterials[r.Next(grassMaterials.Length)].ID;
				}

				terrain.SetTileDataNoHeight(p, td);
			});
		}
	}
}
