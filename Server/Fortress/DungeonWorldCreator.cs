using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.TerrainGen;

namespace Dwarrowdelf.Server.Fortress
{
	public class DungeonWorldCreator
	{
		const int MAP_SIZE = 7;	// 2^AREA_SIZE
		const int MAP_DEPTH = 5;

		public static void InitializeWorld(World world)
		{
			Dictionary<int, IntGrid2[]> roomsDict;

			var terrain = CreateTerrain(out roomsDict);

			IntPoint3? stairs = null;

			foreach (var p2 in terrain.Size.Plane.Range())
			{
				var z = terrain.GetHeight(p2);

				var p = new IntPoint3(p2, z);
				var td = terrain.GetTileData(p);
				if (td.TerrainID == TerrainID.StairsDown)
				{
					stairs = p;
					break;
				}
			}

			if (stairs.HasValue == false)
				throw new Exception();

			var env = EnvironmentObject.Create(world, terrain, VisibilityMode.LivingLOS, stairs.Value);

			CreateMonsters(env, roomsDict);

			CreateDebugMonsterAtEntry(env);
		}

		static void CreateDebugMonsterAtEntry(EnvironmentObject env)
		{
			var pn = GetLocNearEntry(env);
			if (pn.HasValue)
			{
				var living = CreateRandomLiving(env.World, 1);
				living.MoveTo(env, pn.Value);
			}
		}

		static void CreateMonsters(EnvironmentObject env, Dictionary<int, IntGrid2[]> roomsDict)
		{
			foreach (var kvp in roomsDict)
			{
				int z = kvp.Key;
				var rooms = kvp.Value;

				for (int i = 0; i < 10; ++i)
				{
					var room = new IntGrid2Z(rooms[Helpers.GetRandomInt(rooms.Length)], z);

					var pn = GetRandomRoomLoc(env, ref room);
					if (pn.HasValue == false)
						continue;

					var p = pn.Value;

					var living = CreateRandomLiving(env.World, z);
					living.MoveTo(env, p);
				}
			}
		}

		static IntPoint3? GetRandomRoomLoc(EnvironmentObject env, ref IntGrid2Z grid)
		{
			int x = grid.X + Helpers.GetRandomInt(grid.Columns);
			int y = grid.Y + Helpers.GetRandomInt(grid.Rows);

			foreach (var p in IntPoint2.SquareSpiral(new IntPoint2(x, y), Math.Max(grid.Columns, grid.Rows)))
			{
				if (env.Size.Plane.Contains(p) == false)
					continue;

				var p3 = new IntPoint3(p, grid.Z);

				if (EnvironmentHelpers.CanEnter(env, p3) == false)
					continue;

				return p3;
			}

			return null;
		}

		static LivingObject CreateRandomLiving(World world, int z)
		{
			var livingBuilder = new LivingObjectBuilder(LivingID.Wolf);
			var living = livingBuilder.Create(world);
			living.SetAI(new Dwarrowdelf.AI.MonsterAI(living, 0));
			return living;
		}

		static IntPoint3? GetLocNearEntry(EnvironmentObject env)
		{
			foreach (var p in IntPoint2.SquareSpiral(env.StartLocation.ToIntPoint(), env.Width / 2))
			{
				if (env.Size.Plane.Contains(p) == false)
					continue;

				var z = env.GetDepth(p);

				var p3 = new IntPoint3(p, z);

				if (EnvironmentHelpers.CanEnter(env, p3))
					return p3;
			}

			return null;
		}

		static TerrainData CreateTerrain(out Dictionary<int, IntGrid2[]> rooms)
		{
			var random = Helpers.Random;

			int side = (int)Math.Pow(2, MAP_SIZE);
			var size = new IntSize3(side, side, MAP_DEPTH);

			var terrain = new TerrainData(size);

			var tg = new DungeonTerrainGenerator(terrain, random);

			tg.Generate(1);

			TerrainHelpers.CreateSoil(terrain, 9999);
			TerrainHelpers.CreateGrass(terrain, random, 9999);
			TerrainHelpers.CreateTrees(terrain, random);

			rooms = tg.Rooms;

			return terrain;
		}
	}
}
