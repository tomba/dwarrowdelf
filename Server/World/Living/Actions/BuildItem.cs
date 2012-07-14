using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(BuildItemAction action)
		{
			var building = this.Environment.GetLargeObjectAt<BuildingObject>(this.Location);
			if (building == null)
				throw new Exception();

			var buildableItem = building.BuildingInfo.FindBuildableItem(action.BuildableItemKey);
			if (buildableItem == null)
				throw new Exception();

			return GetTicks(buildableItem.SkillID);
		}

		bool PerformAction(BuildItemAction action)
		{
			var building = this.Environment.GetLargeObjectAt<BuildingObject>(this.Location);

			var report = new BuildItemActionReport(this, action.BuildableItemKey);

			if (building == null)
			{
				SendFailReport(report, "cannot find building");
				return false;
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

			if (item != null)
			{
				report.ItemObjectID = item.ObjectID;
				SendReport(report);
			}
			else
			{
				SendFailReport(report, "unable to build the item");
			}

			return item != null;
		}

	}
}
