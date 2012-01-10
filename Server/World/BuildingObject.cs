using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	[SaveGameObjectByRef]
	public sealed class BuildingObject : AreaObject, IBuildingObject
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

		protected override void CollectObjectData(BaseGameObjectData baseData, ObjectVisibility visibility)
		{
			base.CollectObjectData(baseData, visibility);

			var data = (BuildingData)baseData;

			data.BuildingID = this.BuildingInfo.BuildingID;
			data.Area = this.Area;
			data.Environment = this.Environment.ObjectID;
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			Debug.Assert(visibility != ObjectVisibility.None);

			var data = new BuildingData();

			CollectObjectData(data, visibility);

			player.Send(new Messages.ObjectDataMessage(data));
		}

		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();
			return props;
		}

		public bool VerifyBuildItem(LivingObject builder, BuildableItem buildableItem, IEnumerable<ObjectID> sourceObjects)
		{
			if (!Contains(builder.Location))
				return false;

			var srcArray = sourceObjects.Select(oid => this.World.FindObject<ItemObject>(oid)).ToArray();

			if (srcArray.Any(o => o == null || !this.Contains(o.Location)))
				return false;

			return buildableItem.MatchItems(srcArray);
		}

		public ItemObject PerformBuildItem(LivingObject builder, BuildableItem buildableItem, IEnumerable<ObjectID> sourceObjects)
		{
			if (!VerifyBuildItem(builder, buildableItem, sourceObjects))
				return null;

			var obs = sourceObjects.Select(oid => this.World.FindObject<ItemObject>(oid));

			MaterialID materialID;

			if (buildableItem.MaterialID.HasValue)
				materialID = buildableItem.MaterialID.Value;
			else
				materialID = obs.First().MaterialID;

			var skillLevel = builder.GetSkillLevel(buildableItem.SkillID);

			var itemBuilder = new ItemObjectBuilder(buildableItem.ItemID, materialID)
			{
				Quality = skillLevel,
			};
			var item = itemBuilder.Create(this.World);

			foreach (var ob in obs)
				ob.Destruct();

			if (item.MoveTo(builder.Environment, builder.Location) == false)
				throw new Exception();

			return item;
		}

		public static bool VerifyBuildSite(EnvironmentObject env, IntRectZ area)
		{
			return area.Range().All(p => env.GetTerrainID(p) == TerrainID.NaturalFloor);
		}
	}

	public sealed class BuildingObjectBuilder
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
