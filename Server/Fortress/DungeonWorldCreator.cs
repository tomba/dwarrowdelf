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
			var terrain = CreateTerrain();

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


			foreach (var p in IntPoint2.SquareSpiral(env.StartLocation.ToIntPoint(), env.Width / 2))
			{
				if (env.Size.Plane.Contains(p) == false)
					continue;

				var z = env.GetDepth(p);

				var p3 = new IntPoint3(p, z);

				if (EnvironmentHelpers.CanEnter(env, p3))
				{
					var livingBuilder = new LivingObjectBuilder(LivingID.Wolf);
					var living = livingBuilder.Create(world);
					living.SetAI(new Dwarrowdelf.AI.MonsterAI(living, 0));
					living.MoveTo(env, p3);

					break;
				}
			}

		}

		static TerrainData CreateTerrain()
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

			return terrain;
		}
	}
}
