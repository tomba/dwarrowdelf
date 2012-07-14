using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(ConsumeAction action)
		{
			return 6;
		}

		bool PerformAction(ConsumeAction action)
		{
			var ob = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);
			var item = ob as ItemObject;

			if (item == null)
			{
				SendFailReport(new ConsumeActionReport(this, null), "not in inventory");
				return false;
			}

			var refreshment = item.RefreshmentValue;
			var nutrition = item.NutritionalValue;

			if (refreshment == 0 && nutrition == 0)
			{
				SendFailReport(new ConsumeActionReport(this, item), "non-digestible");
				return false;
			}

			// Send report before destruct
			SendReport(new ConsumeActionReport(this, item));

			ob.Destruct();

			this.WaterFullness += refreshment;
			this.FoodFullness += nutrition;

			return true;
		}

	}
}
