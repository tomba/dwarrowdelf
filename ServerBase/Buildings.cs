using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame.Server
{
	public class BuildingData : BaseGameObject
	{
		public BuildingInfo BuildingInfo { get; private set; }
		public Environment Environment { get; set; }
		public int Z { get; set; }
		public IntRect Area { get; set; }

		public BuildingData(World world, BuildingID id)
			: base(world)
		{
			this.BuildingInfo = world.AreaData.Buildings.GetBuildingInfo(id);
		}

		public override MyGame.ClientMsgs.Message Serialize()
		{
			return new ClientMsgs.BuildingData()
			{
				ObjectID = this.ObjectID,
				ID = this.BuildingInfo.ID,
				Area = this.Area,
				Z = this.Z,
				Environment = this.Environment.ObjectID,
			};
		}
	}
}
