using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGame;

namespace MyArea
{
	public class Area : IArea
	{
		public void InitializeWorld(World world)
		{
			// XXX some size limit with the size in WCF
			var env = new MyGame.Environment(world, 100, 100, VisibilityMode.LOS);

			Random r = new Random(123);
			TerrainInfo floor = world.AreaData.Terrains.Single(t => t.Name == "Dungeon Floor");
			TerrainInfo wall = world.AreaData.Terrains.Single(t => t.Name == "Dungeon Wall");
			for (int y = 0; y < env.Height; y++)
			{
				for (int x = 0; x < env.Width; x++)
				{
					if (x < 7 && y < 7)
						env.SetTerrain(new IntPoint3D(x, y, 0), floor.ID);
					else if (r.Next() % 8 == 0)
						env.SetTerrain(new IntPoint3D(x, y, 0), wall.ID);
					else
						env.SetTerrain(new IntPoint3D(x, y, 0), floor.ID);
				}
			}


			var obs = world.AreaData.Objects;

			var rand = new Random();
			for (int i = 0; i < 10; ++i)
			{
				// Add a monster
				var monster = new Living(world);
				monster.SymbolID = obs.Single(o => o.Name == "Monster").SymbolID;
				monster.Name = String.Format("monsu{0}", i);
				if (monster.MoveTo(env, new IntPoint3D(6, 6, 0)) == false)
					throw new Exception();
				var monsterAI = new MonsterActor(monster);
				monster.Actor = monsterAI;
				monster.Color = new GameColor((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256));
			}

			// Add an item
			var item = new ItemObject(world);
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID; ;
			item.Name = "red gem";
			item.Color = GameColors.Red;
			item.MoveTo(env, new IntPoint3D(3, 0, 0));

			item = new ItemObject(world);
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID; ;
			item.Name = "gem";
			item.MoveTo(env, new IntPoint3D(2, 0, 0));

			item = new ItemObject(world);
			item.SymbolID = obs.Single(o => o.Name == "Tree").SymbolID; ;
			item.Name = "puu";
			item.MoveTo(env, new IntPoint3D(0, 3, 0));


			world.Map = env;
		}
	}
}
