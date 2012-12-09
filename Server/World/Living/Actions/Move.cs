using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(MoveAction action)
		{
			var dst = this.Location + action.Direction;

			if (this.ActionTicksUsed == 1)
			{
				if (this.Environment.Contains(dst) == false)
				{
					SendFailReport(new MoveActionReport(this, action.Direction), "could not move (outside map)");
					return ActionState.Fail;
				}

				// total ticks = number of livings on the destination tile + 1
				this.ActionTotalTicks = this.Environment.GetContents(dst).OfType<LivingObject>().Count() + 1;
			}

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

			// drop the carried item if we no longer haul
			if (this.CarriedItem != null)
			{
				var moveOk = this.CarriedItem.MoveTo(this.Environment, this.Location);

				Debug.Assert(moveOk);

				if (!moveOk)
					Trace.TraceWarning("unable to drop carried item");

				this.CarriedItem = null;
			}

			var ok = MoveDir(action.Direction);

			if (!ok)
			{
				SendFailReport(new MoveActionReport(this, action.Direction), "could not move (blocked?)");
				return ActionState.Fail;
			}

			SendReport(new MoveActionReport(this, action.Direction));
			return ActionState.Done;
		}
	}
}
