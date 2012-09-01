using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;
using Dwarrowdelf.Server;

using Dwarrowdelf.TerrainGen;
using System.Threading.Tasks;
using System.Threading;

namespace Dwarrowdelf.Server.Fortress
{
	public static class MountainWorldCreator
	{
		const int MAP_SIZE = 8;	// 2^AREA_SIZE
		const int MAP_DEPTH = 20;

		public static void InitializeWorld(World world)
		{
			var terrain = CreateTerrain();

			var env = EnvironmentObject.Create(world, terrain, VisibilityMode.GlobalFOV);

			MountainWorldPopulator.FinalizeEnv(env);
		}

		static TerrainData CreateTerrain()
		{
			int side = (int)Math.Pow(2, MAP_SIZE);
			var size = new IntSize3(side, side, MAP_DEPTH);

			var terrain = new TerrainData(size);

			var tg = new TerrainGenerator(terrain, Helpers.Random);

			var corners = new DiamondSquare.CornerData()
			{
				NE = 15,
				NW = 10,
				SW = 10,
				SE = 10,
			};

			tg.Generate(corners, 5, 0.75, 1, 2);

			CreateGrass(terrain);

			CreateTrees(terrain);

			return terrain;
		}

		static void CreateGrass(TerrainData terrain)
		{
			var grid = terrain.TileGrid;
			var heightMap = terrain.HeightMap;

			int grassLimit = terrain.Depth * 4 / 5;

			int w = terrain.Width;
			int h = terrain.Height;

			var materials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();
			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					int z = heightMap[y, x];

					var p = new IntPoint3(x, y, z);

					if (z < grassLimit)
					{
						var td = grid[p.Z, p.Y, p.X];

						if (Materials.GetMaterial(td.TerrainMaterialID).Category == MaterialCategory.Soil)
						{
							td.InteriorID = InteriorID.Grass;
							td.InteriorMaterialID = materials[Helpers.GetRandomInt(materials.Length)].ID;

							grid[p.Z, p.Y, p.X] = td;
						}
					}
				}
			}
		}

		static void CreateTrees(TerrainData terrain)
		{
			var grid = terrain.TileGrid;
			var heightMap = terrain.HeightMap;

			var materials = Materials.GetMaterials(MaterialCategory.Wood).ToArray();

			int baseSeed = Helpers.GetRandomInt();
			if (baseSeed == 0)
				baseSeed = 1;

			terrain.Size.Plane.Range().AsParallel().ForAll(p2d =>
			{
				int z = heightMap[p2d.Y, p2d.X];

				var p = new IntPoint3(p2d, z);

				var td = grid[p.Z, p.Y, p.X];

				if (td.InteriorID == InteriorID.Grass)
				{
					var r = new MWCRandom(p, baseSeed);

					if (r.Next(8) == 0)
					{
						td.InteriorID = r.Next(2) == 0 ? InteriorID.Tree : InteriorID.Sapling;
						td.InteriorMaterialID = materials[r.Next(materials.Length)].ID;
						grid[p.Z, p.Y, p.X] = td;
					}
				}
			});
		}

		static void ClearTile(EnvironmentObject env, IntPoint3 p)
		{
			var td = env.GetTileData(p);

			td.TerrainID = TerrainID.Empty;
			td.TerrainMaterialID = MaterialID.Undefined;
			td.InteriorID = InteriorID.Empty;
			td.InteriorMaterialID = MaterialID.Undefined;

			env.SetTileData(p, td);
		}

		static void ClearInside(EnvironmentObject env, IntPoint3 p)
		{
			var td = env.GetTileData(p);

			td.TerrainID = TerrainID.NaturalFloor;
			td.TerrainMaterialID = MaterialID.Granite;
			td.InteriorID = InteriorID.Empty;
			td.InteriorMaterialID = MaterialID.Undefined;

			env.SetTileData(p, td);
		}

		static void CreateWalls(EnvironmentObject env, IntGrid2Z area)
		{
			for (int x = area.X1; x <= area.X2; ++x)
			{
				for (int y = area.Y1; y <= area.Y2; ++y)
				{
					if (y != area.Y1 && y != area.Y2 && x != area.X1 && x != area.X2)
						continue;

					var p = new IntPoint3(x, y, area.Z);

					var td = env.GetTileData(p);

					td.TerrainID = TerrainID.NaturalWall;
					td.TerrainMaterialID = MaterialID.Granite;
					td.InteriorID = InteriorID.Empty;
					td.InteriorMaterialID = MaterialID.Undefined;

					env.SetTileData(p, td);
				}
			}
		}

		static void CreateWater(EnvironmentObject env, IntGrid2Z area)
		{
			for (int x = area.X1; x <= area.X2; ++x)
			{
				for (int y = area.Y1; y <= area.Y2; ++y)
				{
					var p = new IntPoint3(x, y, area.Z);
					env.SetWaterLevel(p, TileData.MaxWaterLevel);
				}
			}
		}

		static void CreateWaterTest(EnvironmentObject env)
		{
			var pos = env.GetSurface(10, 30);
			int surface = pos.Z;

			CreateWalls(env, new IntGrid2Z(pos.X, pos.Y, 3, 8, surface));
			CreateWater(env, new IntGrid2Z(pos.X + 1, pos.Y + 1, 1, 6, surface));

			int x = 15;
			int y = 30;

			ClearTile(env, new IntPoint3(x, y, surface - 0));
			ClearTile(env, new IntPoint3(x, y, surface - 1));
			ClearTile(env, new IntPoint3(x, y, surface - 2));
			ClearTile(env, new IntPoint3(x, y, surface - 3));
			ClearTile(env, new IntPoint3(x, y, surface - 4));
			ClearInside(env, new IntPoint3(x + 0, y, surface - 5));
			ClearInside(env, new IntPoint3(x + 1, y, surface - 5));
			ClearInside(env, new IntPoint3(x + 2, y, surface - 5));
			ClearInside(env, new IntPoint3(x + 3, y, surface - 5));
			ClearInside(env, new IntPoint3(x + 4, y, surface - 5));
			ClearTile(env, new IntPoint3(x + 4, y, surface - 4));
			ClearTile(env, new IntPoint3(x + 4, y, surface - 3));
			ClearTile(env, new IntPoint3(x + 4, y, surface - 2));
			ClearTile(env, new IntPoint3(x + 4, y, surface - 1));
			ClearTile(env, new IntPoint3(x + 4, y, surface - 0));

			{
				// Add a water generator
				var item = WaterGenerator.Create(env.World);
				item.MoveTo(env, new IntPoint3(pos.X + 1, pos.Y + 2, surface));
			}
		}
	}
}
