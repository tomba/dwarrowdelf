#define CACHE_TERRAIN

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;
using Dwarrowdelf.Server;

using Dwarrowdelf.TerrainGen;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Dwarrowdelf.Server.Fortress
{
	public static class FortressWorldCreator
	{
		const int MAP_SIZE = 8;	// 2^AREA_SIZE
		const int MAP_DEPTH = 20;

		public static EnvironmentObject InitializeWorld(World world)
		{
			int side = MyMath.Pow2(MAP_SIZE);
			var size = new IntSize3(side, side, MAP_DEPTH);

#if CACHE_TERRAIN
			var terrain = CreateOrLoadTerrain(size);
#else
			var terrain = CreateTerrain(size);
#endif

			// XXX this is where WorldPopulator creates some buildings
			var p2 = new IntVector2(terrain.Width / 2, terrain.Height / 2);
			var startLoc = terrain.GetSurfaceLocation(p2);

			var env = EnvironmentObject.Create(world, terrain, VisibilityMode.GlobalFOV, startLoc);

			//CreateWaterTest(env);
			//CreateWaterRiverTest(env);

			FortressWorldPopulator.FinalizeEnv(env);

			return env;
		}

		static TerrainData CreateOrLoadTerrain(IntSize3 size)
		{
			var path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "save");
			string file = Path.Combine(path, "terrain-cache-hack.dat");

			TerrainData terrain = null;

			try
			{
				var sw = Stopwatch.StartNew();
				terrain = TerrainData.LoadTerrain(file, "fortress", size);
				sw.Stop();
				Trace.TraceInformation("Load cached terrain {0} ms", sw.ElapsedMilliseconds);
			}
			catch (Exception e)
			{
				Trace.TraceError("Failed to load cached terrain: {0}", e.Message);
			}

			if (terrain == null)
			{
				terrain = CreateTerrain(size);
				var sw = Stopwatch.StartNew();
				terrain.SaveTerrain(file, "fortress");
				sw.Stop();
				Trace.TraceInformation("Save cached terrain {0} ms", sw.ElapsedMilliseconds);
			}
			return terrain;
		}

		static TerrainData CreateTerrain(IntSize3 size)
		{
			//var random = Helpers.Random;
			var random = new Random(1);

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
			var p = new IntVector3(111, 44, 9);
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

				ClearTile(env, new IntVector3(x, y, surface - 0));
				ClearTile(env, new IntVector3(x, y, surface - 1));
				ClearTile(env, new IntVector3(x, y, surface - 2));
				ClearTile(env, new IntVector3(x, y, surface - 3));
				ClearTile(env, new IntVector3(x, y, surface - 4));
				ClearInside(env, new IntVector3(x + 0, y, surface - 5));
				ClearInside(env, new IntVector3(x + 1, y, surface - 5));
				ClearInside(env, new IntVector3(x + 2, y, surface - 5));
				ClearInside(env, new IntVector3(x + 3, y, surface - 5));
				ClearInside(env, new IntVector3(x + 4, y, surface - 5));
				ClearTile(env, new IntVector3(x + 4, y, surface - 4));
				ClearTile(env, new IntVector3(x + 4, y, surface - 3));
				ClearTile(env, new IntVector3(x + 4, y, surface - 2));
				ClearTile(env, new IntVector3(x + 4, y, surface - 1));
				ClearTile(env, new IntVector3(x + 4, y, surface - 0));
			}

			if (true)
			{
				// Add a water generator
				var item = WaterGenerator.Create(env.World);
				item.MoveTo(env, new IntVector3(pos.X + 1, pos.Y + 2, surface));
			}
		}

		public static void ClearTile(EnvironmentObject env, IntVector3 p)
		{
			env.SetTileData(p, TileData.EmptyTileData);
		}

		public static void ClearInside(EnvironmentObject env, IntVector3 p)
		{
			env.SetTileData(p, TileData.GetNaturalFloor(MaterialID.Granite));
		}

		public static void CreateWalls(EnvironmentObject env, IntGrid2Z area)
		{
			for (int x = area.X1; x <= area.X2; ++x)
			{
				for (int y = area.Y1; y <= area.Y2; ++y)
				{
					if (y != area.Y1 && y != area.Y2 && x != area.X1 && x != area.X2)
						continue;

					var p = new IntVector3(x, y, area.Z);

					env.SetTileData(p, TileData.GetNaturalWall(MaterialID.Granite));
				}
			}
		}

		public static void CreateWater(EnvironmentObject env, IntGrid2Z area)
		{
			for (int x = area.X1; x <= area.X2; ++x)
			{
				for (int y = area.Y1; y <= area.Y2; ++y)
				{
					var p = new IntVector3(x, y, area.Z);

					ClearInside(env, p);

					env.SetWaterLevel(p, TileData.MaxWaterLevel);
				}
			}
		}
	}
}
