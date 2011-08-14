using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;

namespace MyArea
{
	[SaveGameObject(UseRef = true)]
	public class FoodGenerator : ItemObject
	{
		public static FoodGenerator Create(World world)
		{
			var builder = new ItemObjectBuilder(ItemID.Contraption, MaterialID.Diamond)
			{
				Name = "Food Generator",
				Color = GameColor.Gold,
			};

			var item = new FoodGenerator(builder);
			item.Initialize(world);
			return item;
		}

		FoodGenerator(ItemObjectBuilder builder)
			: base(builder)
		{
		}

		FoodGenerator(SaveGameContext ctx)
			: base(ctx)
		{
			this.World.TickStarting += OnTickStart;
		}

		protected override void Initialize(World world)
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
				var builder = new ItemObjectBuilder(ItemID.Food, Dwarrowdelf.MaterialID.Flesh)
				{
					Color = GameColor.Green,
					NutritionalValue = 200,
				};
				var ob = builder.Create(this.World);

				var ok = ob.MoveTo(this.Parent, this.Location);
				if (!ok)
					ob.Destruct();
			}

			if (!this.Environment.GetContents(this.Location).OfType<ItemObject>().Any(o => o.ItemID == Dwarrowdelf.ItemID.Drink))
			{
				var builder = new ItemObjectBuilder(ItemID.Drink, Dwarrowdelf.MaterialID.Water)
				{
					Color = GameColor.Aquamarine,
					RefreshmentValue = 200,
				};
				var ob = builder.Create(this.World);

				var ok = ob.MoveTo(this.Parent, this.Location);
				if (!ok)
					ob.Destruct();
			}

		}
	}
}
