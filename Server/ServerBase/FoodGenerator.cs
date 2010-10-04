using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public class FoodGenerator : ItemObject
	{
		public FoodGenerator(World world)
			: base(world)
		{
			world.TickStartEvent += OnTickStart;
			this.Name = "Food Generator";
			this.SymbolID = SymbolID.Contraption;
			this.Color = GameColor.Gold;
		}

		void OnTickStart()
		{
			if (this.Environment == null)
				return;

			if (this.Environment.GetContents(this.Location).Any(o => o.Name == "Food"))
				return;

			var food = new ItemObject(this.World)
			{
				Name = "Food",
				SymbolID = SymbolID.Consumable,
				Color = GameColor.Green,
				NutritionalValue = 50,
				RefreshmentValue = 50,
				MaterialID = MaterialID.Undefined,
			};

			var ok = food.MoveTo(this.Parent, this.Location);
			if (!ok)
				food.Destruct();
		}

		public override void Destruct()
		{
			this.World.TickStartEvent -= OnTickStart;

			base.Destruct();
		}
	}
}
