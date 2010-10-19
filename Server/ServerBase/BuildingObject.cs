using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public class BuildingObject : BaseGameObject, IBuildingObject
	{
		public BuildingInfo BuildingInfo { get; private set; }
		public Environment Environment { get; private set; }
		IEnvironment IBuildingObject.Environment { get { return this.Environment as IEnvironment; } }
		public int Z { get; set; }
		public IntRect Area { get; set; }

		public BuildingObject(BuildingID id)
		{
			this.BuildingInfo = Buildings.GetBuildingInfo(id);
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

		public bool VerifyBuildItem(Living builder, IEnumerable<ObjectID> sourceObjects, ItemType dstItemID)
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

			var sourceItemPossibilities = Dwarrowdelf.Items.GetItem(dstItemID).SourceItemPossibilities;

			if (sourceItemPossibilities == null)
				return false;

			foreach (var choice in sourceItemPossibilities)
			{
				if (srcArray.Length != choice.Length)
					continue;

				bool ok = true;

				for (int i = 0; i < choice.Length; ++i)
				{
					var ob = srcArray[i];

					if (!choice[i].MatchItem(ob))
					{
						ok = false;
						break;
					}
				}

				if (ok)
					return true;
			}

			return false;
		}


		public bool PerformBuildItem(Living builder, IEnumerable<ObjectID> sourceObjects, ItemType dstItemID)
		{
			if (!VerifyBuildItem(builder, sourceObjects, dstItemID))
				return false;

			var obs = sourceObjects.Select(oid => this.World.FindObject<ItemObject>(oid));

			ItemObject item = new ItemObject(dstItemID)
			{
				Name = dstItemID.ToString(),
				MaterialID = obs.First().MaterialID,
			};
			item.Initialize(this.World);

			foreach (var ob in obs)
				ob.Destruct();

			if (item.MoveTo(builder.Environment, builder.Location) == false)
				throw new Exception();

			return true;
		}
	}
}
