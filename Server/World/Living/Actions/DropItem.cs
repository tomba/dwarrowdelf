using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(DropItemAction action)
		{
			return 1;
		}

		bool PerformAction(DropItemAction action)
		{
			if (this.Environment == null)
			{
				SendFailReport(new DropItemActionReport(this, null), "no environment");
				return false;
			}

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (item == null)
			{
				SendFailReport(new DropItemActionReport(this, item), "item not found");
				return false;
			}

			if (item.Parent != this)
			{
				SendFailReport(new DropItemActionReport(this, item), "not in inventory");
				return false;
			}

			if (item.IsWorn)
			{
				SendFailReport(new DropItemActionReport(this, item), "item worn");
				return false;
			}

			if (item.IsWielded)
			{
				SendFailReport(new DropItemActionReport(this, item), "item wielded");
				return false;
			}

			if (item.MoveTo(this.Environment, this.Location) == false)
			{
				SendFailReport(new DropItemActionReport(this, item), "failed to move");
				return false;
			}

			if (this.CarriedItem == item)
				this.CarriedItem = null;

			SendReport(new DropItemActionReport(this, item));

			return true;
		}

	}
}
