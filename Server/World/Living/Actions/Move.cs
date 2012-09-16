using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(MoveAction action)
		{
			var dst = this.Location + action.Direction;
			if (this.Environment.Contains(dst) == false)
			{
				SendFailReport(new MoveActionReport(this, action.Direction), "could not move (outside map)");
				return -1;
			}

			var obs = this.Environment.GetContents(dst);
			return obs.OfType<LivingObject>().Count() + 1;
		}

		bool PerformAction(MoveAction action)
		{
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
			}
			else
			{
				SendReport(new MoveActionReport(this, action.Direction));
			}

			return ok;
		}

	}
}
