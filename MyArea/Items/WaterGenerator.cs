using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;

namespace MyArea
{
	[SaveGameObject(UseRef = true)]
	public class WaterGenerator : ItemObject
	{
		public WaterGenerator()
			: base(ItemID.Custom, MaterialID.Diamond)
		{
			this.Name = "Water Generator";
			this.SymbolID = SymbolID.Contraption;
			this.Color = GameColor.Blue;
		}

		WaterGenerator(GameSerializationContext ctx)
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

			this.Environment.SetWaterLevel(this.Location, TileData.MaxWaterLevel);
		}
	}
}
