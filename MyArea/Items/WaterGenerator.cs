using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;

namespace MyArea
{
	[GameObject(UseRef = true)]
	public class WaterGenerator : ItemObject
	{
		public WaterGenerator()
			: base(ItemID.Custom, MaterialID.Diamond)
		{
			this.Name = "Water Generator";
			this.SymbolID = SymbolID.Contraption;
			this.Color = GameColor.Blue;
		}

		[OnGameDeserialized]
		void OnDeserialized()
		{
			this.World.TickStartEvent += OnTickStart;
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
