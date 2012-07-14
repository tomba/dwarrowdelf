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
			var obs = this.Environment.GetContents(this.Location + action.Direction);
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
