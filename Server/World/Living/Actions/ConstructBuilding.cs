using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(ConstructBuildingAction action)
		{
			return 10;
		}

		bool PerformAction(ConstructBuildingAction action)
		{
			var env = this.World.FindObject<EnvironmentObject>(action.EnvironmentID);

			var report = new ConstructBuildingActionReport(this, action.BuildingID);

			if (env == null)
			{
				SendFailReport(report, "no environment specified");
				return false;
			}

			if (!action.Area.Contains(this.Location))
			{
				SendFailReport(report, "not at the construction site");
				return false;
			}

			if (BuildingObject.VerifyBuildSite(env, action.Area) == false)
			{
				SendFailReport(report, "construction site not clean");
				return false;
			}

			var builder = new BuildingObjectBuilder(action.BuildingID, action.Area);
			var building = builder.Create(this.World, env);

			SendReport(report);

			return true;
		}
	}
}
