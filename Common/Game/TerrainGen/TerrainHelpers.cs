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
		public static void CreateSoil(TerrainData data, int soilLimit)
		{
			int w = data.Width;
			int h = data.Height;

			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					int z = data.GetHeight(x, y);

					var p = new IntPoint3(x, y, z);

					if (z < soilLimit)
					{
						var td = data.GetTileData(p);

						td.TerrainMaterialID = MaterialID.Loam;

						data.SetTileDataNoHeight(p, td);
					}
				}
			}
		}

		public static void CreateSlopes(TerrainData data, int baseSeed)
		{
			var plane = data.Size.Plane;

			plane.Range().AsParallel().ForAll(p =>
			{
				int z = data.GetHeight(p);

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

					if (plane.Contains(t) && data.GetHeight(t) > z)
					{
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

					var td = data.GetTileData(p3d);
					td.TerrainID = dir.ToSlope();
					data.SetTileDataNoHeight(p3d, td);
				}
			});
		}

		public static void CreateVegetation(TerrainData terrain, Random random, int vegetationLimit)
		{
			var grid = terrain.TileGrid;
			var heightMap = terrain.HeightMap;

			int w = terrain.Width;
			int h = terrain.Height;

			var grassMaterials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();
			var woodMaterials = Materials.GetMaterials(MaterialCategory.Wood).ToArray();
			var berryMaterials = Materials.GetMaterials(MaterialCategory.Berry).ToArray();

			int baseSeed = random.Next();
			if (baseSeed == 0)
				baseSeed = 1;

			terrain.Size.Plane.Range().AsParallel().ForAll(p2d =>
			{
				int z = heightMap[p2d.Y, p2d.X];

				var p = new IntPoint3(p2d, z);

				if (z >= vegetationLimit)
					return;

				var td = grid[p.Z, p.Y, p.X];

				if (td.WaterLevel > 0)
					return;

				if (Materials.GetMaterial(td.TerrainMaterialID).Category != MaterialCategory.Soil)
					return;

				if (td.TerrainID.IsFloor() == false && td.TerrainID.IsSlope() == false)
					return;

				var r = new MWCRandom(p, baseSeed);

				int v = r.Next(100);

				if (v >= 95)
				{
					td.InteriorID = InteriorID.Sapling;
					td.InteriorMaterialID = woodMaterials[r.Next(woodMaterials.Length)].ID;
				}
				else if (v >= 90)
				{
					td.InteriorID = InteriorID.Tree;
					td.InteriorMaterialID = woodMaterials[r.Next(woodMaterials.Length)].ID;
				}
				else if (v >= 80)
				{
					td.InteriorID = InteriorID.Shrub;
					td.InteriorMaterialID = berryMaterials[r.Next(berryMaterials.Length)].ID;
				}
				else
				{
					td.InteriorID = InteriorID.Grass;
					td.InteriorMaterialID = grassMaterials[r.Next(grassMaterials.Length)].ID;
				}

				grid[p.Z, p.Y, p.X] = td;
			});
		}
	}
}
