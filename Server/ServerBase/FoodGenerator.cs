using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public class FoodGenerator : ItemObject
	{
		public FoodGenerator()
		{
			this.Name = "Food Generator";
			this.SymbolID = SymbolID.Contraption;
			this.Color = GameColor.Gold;
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			world.TickStartEvent += OnTickStart;
		}

		public override void Destruct()
		{
			this.World.TickStartEvent -= OnTickStart;

			base.Destruct();
		}

		void OnTickStart()
		{
			if (this.Environment == null)
				return;

			if (this.Environment.GetContents(this.Location).Any(o => o.Name == "Food"))
				return;

			var food = new ItemObject()
			{
				Name = "Food",
				SymbolID = SymbolID.Consumable,
				Color = GameColor.Green,
				NutritionalValue = 50,
				RefreshmentValue = 50,
				MaterialID = MaterialID.Undefined,
			};
			food.Initialize(this.World);

			var ok = food.MoveTo(this.Parent, this.Location);
			if (!ok)
				food.Destruct();
		}
	}
}
