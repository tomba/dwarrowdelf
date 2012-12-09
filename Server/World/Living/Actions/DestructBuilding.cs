using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(DestructBuildingAction action)
		{
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = 10;

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

			var building = this.World.FindObject<BuildingObject>(action.BuildingID);

			if (building == null)
			{
				SendFailReport(new DestructBuildingActionReport(this, null), "no such building");
				return ActionState.Fail;
			}

			if (!building.Area.Contains(this.Location))
			{
				SendFailReport(new DestructBuildingActionReport(this, building), "not at the building");
				return ActionState.Fail;
			}

			// send report before destruct
			SendReport(new DestructBuildingActionReport(this, building));

			building.Destruct();

			return ActionState.Done;
		}
	}
}
