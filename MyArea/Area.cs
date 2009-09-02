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
			var env = new MyGame.Environment(world, 55, 55, VisibilityMode.AllVisible);

			Random r = new Random(123);
			TerrainInfo floor = world.AreaData.Terrains.Single(t => t.Name == "Dungeon Floor");
			TerrainInfo wall = world.AreaData.Terrains.Single(t => t.Name == "Dungeon Wall");
			for (int y = 0; y < env.Height; y++)
			{
				for (int x = 0; x < env.Width; x++)
				{
					if (x < 7 && y < 7)
						env.SetTerrain(new IntPoint(x, y), floor.ID);
					else if (r.Next() % 8 == 0)
						env.SetTerrain(new IntPoint(x, y), wall.ID);
					else
						env.SetTerrain(new IntPoint(x, y), floor.ID);
				}
			}


			var obs = world.AreaData.Objects;

			for (int i = 0; i < 10; ++i)
			{
				// Add a monster
				var monster = new Living(world);
				monster.SymbolID = obs.Single(o => o.Name == "Monster").SymbolID;
				monster.Name = String.Format("monsu{0}", i);
				if (monster.MoveTo(env, new IntPoint(6, 6)) == false)
					throw new Exception();
				var monsterAI = new MonsterActor(monster);
				monster.Actor = monsterAI;
				monster.Color = (i % 2 == 0) ? new GameColor(255, 0, 0) : new GameColor(0, 255, 0);
			}

			// Add an item
			var item = new ItemObject(world);
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID; ;
			item.Name = "red gem";
			item.Color = new GameColor(255, 0, 0);
			item.MoveTo(env, new IntPoint(3, 0));

			item = new ItemObject(world);
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID; ;
			item.Name = "gem";
			item.MoveTo(env, new IntPoint(2, 0));

			item = new ItemObject(world);
			item.SymbolID = obs.Single(o => o.Name == "Tree").SymbolID; ;
			item.Name = "puu";
			item.MoveTo(env, new IntPoint(0, 3));


			world.Map = env;
		}
	}
}
