using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame.Client
{
	class BuildingData : BaseGameObject
	{
		public BuildingInfo BuildingInfo { get; private set; }
		public Environment Environment { get; set; }
		public int Z { get; set; }
		public IntRect Area { get; set; }

		public BuildingData(World world, ObjectID objectID, BuildingID id)
			: base(world, objectID)
		{
			this.BuildingInfo = world.AreaData.Buildings.GetBuildingInfo(id);
		}
	}
}
