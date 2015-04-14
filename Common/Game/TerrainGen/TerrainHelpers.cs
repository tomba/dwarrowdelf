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
					int z = data.GetSurfaceLevel(x, y);

					var p = new IntVector3(x, y, z);

					if (z < soilLimit)
					{
						var td = data.GetTileData(p);

						td.TerrainMaterialID = MaterialID.Loam;

						data.SetTileDataNoHeight(p, td);
					}
				}
			}
		}

		public static void CreateSlopes(TerrainData data)
		{
			var plane = data.Size.Plane;

			plane.Range().AsParallel().ForAll(p =>
			{
				int z = data.GetSurfaceLevel(p);

				bool create = DirectionSet.Planar.ToSurroundingPoints(p)
					.Any(pp => plane.Contains(pp) && data.GetSurfaceLevel(pp) > z);

				if (create)
				{
					var p3d = new IntVector3(p, z);

					var td = data.GetTileData(p3d);
					td.TerrainID = TerrainID.Slope;
					data.SetTileDataNoHeight(p3d, td);
				}
			});
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

				if (Materials.GetMaterial(td.TerrainMaterialID).Category != MaterialCategory.Soil)
					return;

				if (td.HasFloor == false && td.HasSlope == false)
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

				terrain.SetTileDataNoHeight(p, td);
			});
		}
	}
}
