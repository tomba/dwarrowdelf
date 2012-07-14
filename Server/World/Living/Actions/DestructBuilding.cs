using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(DestructBuildingAction action)
		{
			return 10;
		}

		bool PerformAction(DestructBuildingAction action)
		{
			var building = this.World.FindObject<BuildingObject>(action.BuildingID);

			if (building == null)
			{
				SendFailReport(new DestructBuildingActionReport(this, null), "no such building");
				return false;
			}

			if (!building.Area.Contains(this.Location))
			{
				SendFailReport(new DestructBuildingActionReport(this, building), "not at the building");
				return false;
			}

			// send report before destruct
			SendReport(new DestructBuildingActionReport(this, building));

			building.Destruct();

			return true;
		}

	}
}
