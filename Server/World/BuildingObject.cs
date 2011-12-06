using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	[SaveGameObjectByRef]
	public class BuildingObject : AreaObject, IBuildingObject
	{
		internal static BuildingObject Create(World world, EnvironmentObject env, BuildingObjectBuilder builder)
		{
			var ob = new BuildingObject(builder);
			ob.Initialize(world, env);
			return ob;
		}

		[SaveGameProperty]
		public BuildingID BuildingID { get; private set; }
		public BuildingInfo BuildingInfo { get { return Buildings.GetBuildingInfo(this.BuildingID); } }

		BuildingObject(BuildingObjectBuilder builder)
			: base(ObjectType.Building, builder.Area)
		{
			this.BuildingID = builder.BuildingID;
		}

		BuildingObject(SaveGameContext ctx)
			: base(ctx, ObjectType.Building)
		{
		}

		protected override void Initialize(World world)
		{
			throw new NotImplementedException();
		}

		void Initialize(World world, EnvironmentObject env)
		{
			if (BuildingObject.VerifyBuildSite(env, this.Area) == false)
				throw new Exception();

			SetEnvironment(env);
			base.Initialize(world);
		}

		public override void Destruct()
		{
			SetEnvironment(null);
			base.Destruct();
		}

		protected override void SerializeTo(BaseGameObjectData data, ObjectVisibility visibility)
		{
			base.SerializeTo(data, visibility);

			SerializeToInternal((BuildingData)data, visibility);
		}

		void SerializeToInternal(BuildingData data, ObjectVisibility visibility)
		{
			data.ID = this.BuildingInfo.ID;
			data.Area = this.Area;
			data.Environment = this.Environment.ObjectID;
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			var data = new BuildingData();

			SerializeTo(data, visibility);

			player.Send(new Messages.ObjectDataMessage(data));
		}

		protected override Dictionary<PropertyID, object> SerializeProperties(ObjectVisibility visibility)
		{
			var props = base.SerializeProperties(visibility);
			return props;
		}

		public bool VerifyBuildItem(LivingObject builder, IEnumerable<ObjectID> sourceObjects, ItemID dstItemID)
		{
			if (!Contains(builder.Location))
				return false;

			var srcArray = sourceObjects.Select(oid => this.World.FindObject<ItemObject>(oid)).ToArray();

			if (srcArray.Any(o => o == null || !this.Contains(o.Location)))
				return false;

			switch (this.BuildingInfo.ID)
			{
				case BuildingID.Carpenter:
					if (srcArray[0].MaterialCategory != MaterialCategory.Wood)
						return false;
					break;

				case BuildingID.Mason:
					if (srcArray[0].MaterialCategory != MaterialCategory.Rock)
						return false;
					break;

				default:
					return false;
			}

			return this.BuildingInfo.ItemBuildableFrom(dstItemID, srcArray);
		}


		public bool PerformBuildItem(LivingObject builder, IEnumerable<ObjectID> sourceObjects, ItemID dstItemID)
		{
			if (!VerifyBuildItem(builder, sourceObjects, dstItemID))
				return false;

			var obs = sourceObjects.Select(oid => this.World.FindObject<ItemObject>(oid));

			var itemBuilder = new ItemObjectBuilder(dstItemID, obs.First().MaterialID);
			var item = itemBuilder.Create(this.World);

			foreach (var ob in obs)
				ob.Destruct();

			if (item.MoveTo(builder.Environment, builder.Location) == false)
				throw new Exception();

			return true;
		}

		public static bool VerifyBuildSite(EnvironmentObject env, IntRectZ area)
		{
			return area.Range().All(p => env.GetTerrainID(p) == TerrainID.NaturalFloor);
		}
	}

	public class BuildingObjectBuilder
	{
		public BuildingID BuildingID { get; private set; }
		public IntRectZ Area { get; private set; }

		public BuildingObjectBuilder(BuildingID id, IntRectZ area)
		{
			this.BuildingID = id;
			this.Area = area;
		}

		public BuildingObject Create(World world, EnvironmentObject env)
		{
			return BuildingObject.Create(world, env, this);
		}
	}
}
