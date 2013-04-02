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

			// XXX this is where WorldPopulator creates some buildings
			var p2 = new IntPoint2(terrain.Width / 2, terrain.Height / 2);
			var startLoc = terrain.GetSurfaceLocation(p2);

			var env = EnvironmentObject.Create(world, terrain, VisibilityMode.GlobalFOV, startLoc);

			//CreateWaterTest(env);
			//CreateWaterRiverTest(env);

			MountainWorldPopulator.FinalizeEnv(env);
		}

		static TerrainData CreateTerrain()
		{
			//var random = Helpers.Random;
			var random = new Random(1);

			int side = MyMath.Pow2(MAP_SIZE);
			var size = new IntSize3(side, side, MAP_DEPTH);

			var terrain = new TerrainData(size);

			var tg = new TerrainGenerator(terrain, random);

			var corners = new DiamondSquare.CornerData()
			{
				NE = 15,
				NW = 10,
				SW = 10,
				SE = 10,
			};

			tg.Generate(corners, 5, 0.75, 2);

			int grassLimit = terrain.Depth * 4 / 5;
			TerrainHelpers.CreateVegetation(terrain, random, grassLimit);

			return terrain;
		}

		static void CreateWaterRiverTest(EnvironmentObject env)
		{
			var p = new IntPoint3(111, 44, 9);
			for (int x = 0; x < 6; ++x)
			{
				env.SetTileData(p.Offset(x, 0, 0), new TileData()
				{
					InteriorID = InteriorID.Empty,
					TerrainID = TerrainID.Empty,
				});

				env.SetTileData(p.Offset(x, 0, -1), new TileData()
				{
					InteriorID = InteriorID.Empty,
					TerrainID = TerrainID.NaturalFloor,
					TerrainMaterialID = MaterialID.Granite,
				});
			}
		}

		static void CreateWaterTest(EnvironmentObject env)
		{
			var pos = env.GetSurfaceLocation(env.Width / 2 + 10, env.Height / 2 - 10);

			int surface = pos.Z;

			CreateWalls(env, new IntGrid2Z(pos.X, pos.Y, 5, 8, surface));
			CreateWater(env, new IntGrid2Z(pos.X + 1, pos.Y + 1, 3, 6, surface));
			CreateWalls(env, new IntGrid2Z(pos.X + 2, pos.Y + 2, 1, 4, surface));

			if (true)
			{
				int x = pos.X + 1;
				int y = pos.Y + 1;

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
			}

			if (true)
			{
				// Add a water generator
				var item = WaterGenerator.Create(env.World);
				item.MoveTo(env, new IntPoint3(pos.X + 1, pos.Y + 2, surface));
			}
		}

		public static void ClearTile(EnvironmentObject env, IntPoint3 p)
		{
			var td = env.GetTileData(p);

			td.TerrainID = TerrainID.Empty;
			td.TerrainMaterialID = MaterialID.Undefined;
			td.InteriorID = InteriorID.Empty;
			td.InteriorMaterialID = MaterialID.Undefined;

			env.SetTileData(p, td);
		}

		public static void ClearInside(EnvironmentObject env, IntPoint3 p)
		{
			var td = env.GetTileData(p);

			td.TerrainID = TerrainID.NaturalFloor;
			td.TerrainMaterialID = MaterialID.Granite;
			td.InteriorID = InteriorID.Empty;
			td.InteriorMaterialID = MaterialID.Undefined;

			env.SetTileData(p, td);
		}

		public static void CreateWalls(EnvironmentObject env, IntGrid2Z area)
		{
			for (int x = area.X1; x <= area.X2; ++x)
			{
				for (int y = area.Y1; y <= area.Y2; ++y)
				{
					if (y != area.Y1 && y != area.Y2 && x != area.X1 && x != area.X2)
						continue;

					var p = new IntPoint3(x, y, area.Z);

					var td = new TileData();

					td.TerrainID = TerrainID.NaturalWall;
					td.TerrainMaterialID = MaterialID.Granite;
					td.InteriorID = InteriorID.Empty;
					td.InteriorMaterialID = MaterialID.Undefined;

					env.SetTileData(p, td);
				}
			}
		}

		public static void CreateWater(EnvironmentObject env, IntGrid2Z area)
		{
			for (int x = area.X1; x <= area.X2; ++x)
			{
				for (int y = area.Y1; y <= area.Y2; ++y)
				{
					var p = new IntPoint3(x, y, area.Z);

					ClearInside(env, p);

					env.SetWaterLevel(p, TileData.MaxWaterLevel);
				}
			}
		}
	}
}
