using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	[GameObject(UseRef = true)]
	public class BuildingObject : BaseGameObject, IBuildingObject
	{
		[GameProperty]
		public BuildingID BuildingID { get; private set; }
		public BuildingInfo BuildingInfo { get { return Buildings.GetBuildingInfo(this.BuildingID); } }
		[GameProperty]
		public Environment Environment { get; private set; }
		IEnvironment IBuildingObject.Environment { get { return this.Environment as IEnvironment; } }
		[GameProperty]
		public IntRect3D Area { get; set; }

		BuildingObject()
			: base(ObjectType.Building)
		{
		}

		public BuildingObject(BuildingID id)
			: base(ObjectType.Building)
		{
			this.BuildingID = id;
		}

		public override void Initialize(World world)
		{
			throw new NotImplementedException();
		}

		public void Initialize(World world, Environment env)
		{
			this.Environment = env;
			env.AddBuilding(this);
			base.Initialize(world);
		}

		public override void Destruct()
		{
			this.Environment.RemoveBuilding(this);
			base.Destruct();
		}

		public override BaseGameObjectData Serialize()
		{
			return new BuildingData()
			{
				ObjectID = this.ObjectID,
				ID = this.BuildingInfo.ID,
				Area = this.Area,
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
			return this.Area.Contains(point);
		}

		public bool VerifyBuildItem(Living builder, IEnumerable<ObjectID> sourceObjects, ItemID dstItemID)
		{
			if (!Contains(builder.Location))
				return false;

			var srcArray = sourceObjects.Select(oid => this.World.FindObject<ItemObject>(oid)).ToArray();

			if (srcArray.Any(o => o == null || !this.Contains(o.Location)))
				return false;

			switch (this.BuildingInfo.ID)
			{
				case BuildingID.Carpenter:
					if (srcArray[0].MaterialClass != MaterialClass.Wood)
						return false;
					break;

				case BuildingID.Mason:
					if (srcArray[0].MaterialClass != MaterialClass.Rock)
						return false;
					break;

				default:
					return false;
			}

			return this.BuildingInfo.ItemBuildableFrom(dstItemID, srcArray);
		}


		public bool PerformBuildItem(Living builder, IEnumerable<ObjectID> sourceObjects, ItemID dstItemID)
		{
			if (!VerifyBuildItem(builder, sourceObjects, dstItemID))
				return false;

			var obs = sourceObjects.Select(oid => this.World.FindObject<ItemObject>(oid));

			ItemObject item = new ItemObject(dstItemID, obs.First().MaterialID);
			item.Initialize(this.World);

			foreach (var ob in obs)
				ob.Destruct();

			if (item.MoveTo(builder.Environment, builder.Location) == false)
				throw new Exception();

			return true;
		}
	}
}
