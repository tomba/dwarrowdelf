using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(SleepAction action)
		{
			const int exhaustion_per_tick = 10;

			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = this.Exhaustion / exhaustion_per_tick;

			this.Exhaustion = Math.Max(this.Exhaustion - exhaustion_per_tick, 0);

			if (this.ActionTicksUsed < this.ActionTotalTicks)
			{
				return ActionState.Ok;
			}
			else
			{
				SendReport(new SleepActionReport(this));
				return ActionState.Done;
			}
		}
	}
}
