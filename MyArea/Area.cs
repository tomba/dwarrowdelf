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
			// Add a monster
			var monster = new Living(world);
			monster.SymbolID = 4;
			monster.Name = "monsu";
			monster.MoveTo(world.Map, new IntPoint(2, 2));
			var monsterAI = new MonsterActor(monster);
			monster.Actor = monsterAI;

			// Add an item
			var item = new ItemObject(world);
			item.SymbolID = 5;
			item.Name = "testi-itemi";
			item.MoveTo(world.Map, new IntPoint(1, 1));

			// process changes so that moves above are handled
			world.ProcessChanges();
		}
	}
}
