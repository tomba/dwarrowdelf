using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(HaulAction action)
		{
			var dir = action.Direction;

			var obs = this.Environment.GetContents(this.Location + dir);
			return obs.OfType<LivingObject>().Count() + 2;
		}

		bool PerformAction(HaulAction action)
		{
			var dir = action.Direction;
			var itemID = action.ItemID;
			var item = this.World.FindObject<ItemObject>(itemID);

			var report = new HaulActionReport(this, dir, item);

			if (item == null)
			{
				SendFailReport(report, "object doesn't exist");
				return false;
			}

			if (this.CarriedItem == null)
			{
				SendFailReport(report, "not carrying anything");
				return false;
			}

			Debug.Assert(this.CarriedItem.Parent == this);

			if (this.CarriedItem != item)
			{
				SendFailReport(report, "already carrying another item");
				return false;
			}

			var ok = MoveDir(dir);

			if (!ok)
			{
				SendFailReport(new HaulActionReport(this, action.Direction, item), "could not move (blocked?)");
				return false;
			}

			SendReport(report);

			return true;
		}

	}
}
