using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(HaulAction action)
		{
			var dir = action.Direction;

			if (this.ActionTicksUsed == 1)
			{
				var obs = this.Environment.GetContents(this.Location + dir);

				this.ActionTotalTicks = obs.OfType<LivingObject>().Count() + 2;
			}

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

			var itemID = action.ItemID;
			var item = this.World.FindObject<ItemObject>(itemID);

			var report = new HaulActionReport(this, dir, item);

			if (item == null)
			{
				SendFailReport(report, "object doesn't exist");
				return ActionState.Fail;
			}

			if (this.CarriedItem == null)
			{
				SendFailReport(report, "not carrying anything");
				return ActionState.Fail;
			}

			Debug.Assert(this.CarriedItem.Parent == this);

			if (this.CarriedItem != item)
			{
				SendFailReport(report, "already carrying another item");
				return ActionState.Fail;
			}

			var ok = MoveDir(dir);

			if (!ok)
			{
				SendFailReport(new HaulActionReport(this, action.Direction, item), "could not move (blocked?)");
				return ActionState.Fail;
			}

			SendReport(report);

			return ActionState.Done;
		}
	}
}
