using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;

namespace MyArea
{
	[GameObject(UseRef = true)]
	public class FoodGenerator : ItemObject
	{
		public FoodGenerator()
			: base(ItemID.Custom, MaterialID.Diamond)
		{
			this.Name = "Food Generator";
			this.SymbolID = SymbolID.Contraption;
			this.Color = GameColor.Gold;
		}

		FoodGenerator(GameSerializationContext ctx)
			: base(ctx)
		{
			this.World.TickStarting += OnTickStart;
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			world.TickStarting += OnTickStart;
		}

		public override void Destruct()
		{
			this.World.TickStarting -= OnTickStart;

			base.Destruct();
		}

		void OnTickStart()
		{
			if (this.Environment == null)
				return;

			if (!this.Environment.GetContents(this.Location).OfType<ItemObject>().Any(o => o.ItemID == Dwarrowdelf.ItemID.Food))
			{
				var ob = new ItemObject(ItemID.Food, Dwarrowdelf.MaterialID.Flesh)
				{
					Color = GameColor.Green,
					NutritionalValue = 200,
				};
				ob.Initialize(this.World);

				var ok = ob.MoveTo(this.Parent, this.Location);
				if (!ok)
					ob.Destruct();
			}

			if (!this.Environment.GetContents(this.Location).OfType<ItemObject>().Any(o => o.ItemID == Dwarrowdelf.ItemID.Drink))
			{
				var ob = new ItemObject(ItemID.Drink, Dwarrowdelf.MaterialID.Water)
				{
					Color = GameColor.Aquamarine,
					RefreshmentValue = 200,
				};
				ob.Initialize(this.World);

				var ok = ob.MoveTo(this.Parent, this.Location);
				if (!ok)
					ob.Destruct();
			}

		}
	}
}
