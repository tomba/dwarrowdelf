using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(CarryItemAction action)
		{
			return 1;
		}

		bool PerformAction(CarryItemAction action)
		{
			var item = this.World.FindObject<ItemObject>(action.ItemID);

			var report = new CarryItemActionReport(this, item);

			if (item == null)
			{
				SendFailReport(report, "item not found");
				return false;
			}

			if (this.CarriedItem != null)
			{
				SendFailReport(report, "already carrying an item");
				return false;
			}

			if (item.Environment != this.Environment || item.Location != this.Location)
			{
				SendFailReport(report, "item not there");
				return false;
			}

			if (item.MoveTo(this) == false)
			{
				SendFailReport(report, "failed to move");
				return false;
			}

			this.CarriedItem = item;

			SendReport(report);

			return true;
		}

	}
}
