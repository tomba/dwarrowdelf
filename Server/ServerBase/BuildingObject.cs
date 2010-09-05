using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame.Server
{
	public class BuildingObject : BaseGameObject, IBuildingObject
	{
		public BuildingInfo BuildingInfo { get; private set; }
		public Environment Environment { get; set; }
		IEnvironment IBuildingObject.Environment { get { return this.Environment as IEnvironment; } }
		public int Z { get; set; }
		public IntRect Area { get; set; }

		public BuildingObject(World world, BuildingID id)
			: base(world)
		{
			this.BuildingInfo = Buildings.GetBuildingInfo(id);
		}

		public override BaseGameObjectData Serialize()
		{
			return new BuildingData()
			{
				ObjectID = this.ObjectID,
				ID = this.BuildingInfo.ID,
				Area = this.Area,
				Z = this.Z,
				Environment = this.Environment.ObjectID,
			};
		}

		public override void SerializeTo(Action<Messages.ServerMessage> writer)
		{
			var msg = new Messages.ObjectDataMessage() { ObjectData = Serialize() };
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
				SymbolID = SymbolID.Key,
				MaterialID = iron,
			};

			if (item.MoveTo(builder.Environment, builder.Location) == false)
				throw new Exception();

			return true;
		}
	}
}
