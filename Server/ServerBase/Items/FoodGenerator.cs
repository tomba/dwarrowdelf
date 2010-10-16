using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server.Items
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

			if (!this.Environment.GetContents(this.Location).Any(o => o.Name == "Food"))
			{
				var ob = new ItemObject()
				{
					Name = "Food",
					SymbolID = SymbolID.Consumable,
					Color = GameColor.Green,
					NutritionalValue = 200,
					MaterialID = MaterialID.Undefined,
				};
				ob.Initialize(this.World);

				var ok = ob.MoveTo(this.Parent, this.Location);
				if (!ok)
					ob.Destruct();
			}

			if (!this.Environment.GetContents(this.Location).Any(o => o.Name == "Drink"))
			{
				var ob = new ItemObject()
				{
					Name = "Drink",
					SymbolID = SymbolID.Consumable,
					Color = GameColor.Aquamarine,
					RefreshmentValue = 200,
					MaterialID = MaterialID.Undefined,
				};
				ob.Initialize(this.World);

				var ok = ob.MoveTo(this.Parent, this.Location);
				if (!ok)
					ob.Destruct();
			}

		}
	}
}
