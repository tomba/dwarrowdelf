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

			var env = EnvironmentObject.Create(world, terrain, VisibilityMode.GlobalFOV);
		}

		static TerrainData CreateTerrain()
		{
			int side = (int)Math.Pow(2, MAP_SIZE);
			var size = new IntSize3(side, side, MAP_DEPTH);

			var terrain = new TerrainData(size);

			var tg = new DungeonTerrainGenerator(terrain, Helpers.Random);

			tg.Generate(1);

			return terrain;
		}
	}
}
