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

		public override void SerializeTo(Action<ClientMsgs.Message> writer)
		{
			var msg = Serialize();
			writer(msg);
		}

		public bool Contains(IntPoint3D point)
		{
			return point.Z == this.Z && this.Area.Contains(point.ToIntPoint());
		}

		public bool VerifyBuildItem(Living builder, IEnumerable<ObjectID> sourceObjects)
		{
			if (!Contains(builder.Location))
				return false;

			if (sourceObjects.Count() != 2)
				return false;

			if (!sourceObjects.
				Select(oid => this.World.FindObject<ServerGameObject>(oid)).
				All(o => this.Contains(o.Location) || o.Parent == builder))
				return false;

			return true;
		}

		public bool PerformBuildItem(Living builder, IEnumerable<ObjectID> sourceObjects)
		{
			if (!VerifyBuildItem(builder, sourceObjects))
				return false;

			var obs = sourceObjects.Select(oid => this.World.FindObject<ServerGameObject>(oid));

			foreach (var ob in obs)
			{
				ob.Destruct();
			}

			var iron = Materials.Iron.ID;

			ItemObject item = new ItemObject(this.World)
			{
				Name = "Key",
				SymbolID = this.World.AreaData.Symbols.Single(o => o.Name == "Key").ID,
				MaterialID = iron,
			};

			if (item.MoveTo(builder.Environment, builder.Location) == false)
				throw new Exception();

			return true;
		}
	}
}
