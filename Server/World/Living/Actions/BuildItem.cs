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
				var building = this.Environment.GetLargeObjectAt<BuildingObject>(this.Location);
				if (building == null)
					throw new Exception();

				var buildableItem = building.BuildingInfo.FindBuildableItem(action.BuildableItemKey);
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
				var building = this.Environment.GetLargeObjectAt<BuildingObject>(this.Location);

				var report = new BuildItemActionReport(this, action.BuildableItemKey);

				if (building == null)
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

				var bi = building.BuildingInfo.FindBuildableItem(action.BuildableItemKey);

				var item = building.PerformBuildItem(this, bi, action.SourceObjectIDs);

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
	}
}