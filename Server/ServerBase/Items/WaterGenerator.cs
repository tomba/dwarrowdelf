using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server.Items
{
	public class WaterGenerator : ItemObject
	{
		public WaterGenerator()
			: base(ItemID.Custom, MaterialID.Diamond)
		{
			this.Name = "Water Generator";
			this.SymbolID = SymbolID.Contraption;
			this.Color = GameColor.Blue;
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

			this.Environment.SetWaterLevel(this.Location, TileData.MaxWaterLevel);
		}
	}
}
