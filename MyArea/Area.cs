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
			var obs = world.AreaData.Objects;

			for (int i = 0; i < 10; ++i)
			{
				// Add a monster
				var monster = new Living(world);
				monster.SymbolID = obs.Single(o => o.Name == "Monster").SymbolID;
				monster.Name = String.Format("monsu{0}", i);
				if (monster.MoveTo(world.Map, new IntPoint(6, 6)) == false)
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
			item.MoveTo(world.Map, new IntPoint(3, 0));

			item = new ItemObject(world);
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID; ;
			item.Name = "gem";
			item.MoveTo(world.Map, new IntPoint(2, 0));

			item = new ItemObject(world);
			item.SymbolID = obs.Single(o => o.Name == "Tree").SymbolID; ;
			item.Name = "puu";
			item.MoveTo(world.Map, new IntPoint(0, 3));

		}
	}
}
