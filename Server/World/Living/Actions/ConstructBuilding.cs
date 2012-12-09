using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(ConstructBuildingAction action)
		{
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = 10;

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

			var env = this.World.FindObject<EnvironmentObject>(action.EnvironmentID);

			var report = new ConstructBuildingActionReport(this, action.BuildingID);

			if (env == null)
			{
				SendFailReport(report, "no environment specified");
				return ActionState.Fail;
			}

			if (!action.Area.Contains(this.Location))
			{
				SendFailReport(report, "not at the construction site");
				return ActionState.Fail;
			}

			if (BuildingObject.VerifyBuildSite(env, action.Area) == false)
			{
				SendFailReport(report, "construction site not clean");
				return ActionState.Fail;
			}

			var builder = new BuildingObjectBuilder(action.BuildingID, action.Area);
			var building = builder.Create(this.World, env);

			SendReport(report);
			
			return ActionState.Done;
		}
	}
}
