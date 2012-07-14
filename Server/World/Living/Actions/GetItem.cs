using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(GetItemAction action)
		{
			return 1;
		}

		bool PerformAction(GetItemAction action)
		{
			if (this.Environment == null)
			{
				SendFailReport(new GetItemActionReport(this, null), "no environment");
				return false;
			}

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (item == null)
			{
				SendFailReport(new GetItemActionReport(this, item), "item not found");
				return false;
			}

			if (item.Environment != this.Environment || item.Location != this.Location)
			{
				SendFailReport(new GetItemActionReport(this, item), "item not there");
				return false;
			}

			if (item.MoveTo(this) == false)
			{
				SendFailReport(new GetItemActionReport(this, item), "failed to move");
				return false;
			}

			SendReport(new GetItemActionReport(this, item));

			return true;
		}

	}
}
