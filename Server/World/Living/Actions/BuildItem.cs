using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(BuildItemAction action)
		{
			if (this.ActionTicksUsed == 1)
			{
				var workbench = this.World.FindObject<ItemObject>(action.WorkbenchID);
				if (workbench == null)
					throw new Exception();

				var bi = Buildings.GetBuildItemInfo(workbench.ItemID);

				var buildableItem = bi.FindBuildableItem(action.BuildableItemKey);
				if (buildableItem == null)
					throw new Exception();

				this.ActionTotalTicks = GetTicks(buildableItem.SkillID);
			}

			if (this.ActionTicksUsed < this.ActionTotalTicks)
			{
				return ActionState.Ok;
			}
			else
			{
				var workbench = this.World.FindObject<ItemObject>(action.WorkbenchID);

				var report = new BuildItemActionReport(this, action.BuildableItemKey);

				if (workbench == null)
				{
					SendFailReport(report, "cannot find building");
					return ActionState.Fail;
				}

				/*
							if (this.ActionTicksLeft != 0)
							{
								var ok = building.VerifyBuildItem(this, action.SourceObjectIDs, action.DstItemID);
								if (!ok)
									SetActionError("build item request is invalid");
								return ok;
							}
				 */


				var bi2 = Buildings.GetBuildItemInfo(workbench.ItemID);

				var bi = bi2.FindBuildableItem(action.BuildableItemKey);

				var item = PerformBuildItem(this, bi, action.SourceObjectIDs);

				if (item == null)
				{
					SendFailReport(report, "unable to build the item");
					return ActionState.Fail;
				}

				report.ItemObjectID = item.ObjectID;
				SendReport(report);

				return ActionState.Done;
			}
		}

		static bool VerifyBuildItem(LivingObject builder, BuildableItem buildableItem, IEnumerable<ObjectID> sourceObjects)
		{
			return true;
			/*
			if (!Contains(builder.Location))
				return false;

			var srcArray = sourceObjects.Select(oid => this.World.FindObject<ItemObject>(oid)).ToArray();

			if (srcArray.Any(o => o == null || !this.Contains(o.Location)))
				return false;

			return buildableItem.MatchItems(srcArray);
			 */
		}

		static ItemObject PerformBuildItem(LivingObject builder, BuildableItem buildableItem, IEnumerable<ObjectID> sourceObjects)
		{
			if (!VerifyBuildItem(builder, buildableItem, sourceObjects))
				return null;

			var obs = sourceObjects.Select(oid => builder.World.FindObject<ItemObject>(oid));

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
			var item = itemBuilder.Create(builder.World);

			foreach (var ob in obs)
				ob.Destruct();

			if (item.MoveTo(builder.Environment, builder.Location) == false)
				throw new Exception();

			return item;
		}
	}
}