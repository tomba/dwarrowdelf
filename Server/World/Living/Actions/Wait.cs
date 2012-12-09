using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(WaitAction action)
		{
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = action.WaitTicks;

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;
			else
				return ActionState.Done;
		}
	}
}
