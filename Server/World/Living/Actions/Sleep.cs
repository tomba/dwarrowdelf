using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(SleepAction action)
		{
			return action.SleepTicks;
		}

		bool PerformAction(SleepAction action)
		{
			this.Exhaustion = Math.Max(this.Exhaustion - action.SleepTicks * 10, 0);

			SendReport(new SleepActionReport(this));

			return true;
		}
	}
}
