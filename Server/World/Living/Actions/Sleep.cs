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
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = action.SleepTicks;

			this.Exhaustion = Math.Max(this.Exhaustion - 10, 0);

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
