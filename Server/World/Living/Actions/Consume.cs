using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckConsumeAction(ItemObject item)
		{
			if (item == null)
			{
				SendFailReport(new ConsumeActionReport(this, null), "not in inventory");
				return false;
			}

			if (item.RefreshmentValue == 0 && item.NutritionalValue == 0)
			{
				SendFailReport(new ConsumeActionReport(this, item), "non-digestible");
				return false;
			}

			return true;
		}

		ActionState ProcessAction(ConsumeAction action)
		{
			if (this.ActionTicksUsed == 1)
			{
				var item = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID) as ItemObject;

				if (CheckConsumeAction(item) == false)
					return ActionState.Fail;

				this.ActionTotalTicks = 6;

				return ActionState.Ok;
			}
			else if (this.ActionTicksUsed < this.ActionTotalTicks)
			{
				return ActionState.Ok;
			}
			else
			{
				var item = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID) as ItemObject;

				if (CheckConsumeAction(item) == false)
					return ActionState.Fail;

				// Send report before destruct
				SendReport(new ConsumeActionReport(this, item));

				item.Destruct();

				this.Hunger = Math.Max(this.Hunger - item.NutritionalValue, 0);
				this.Thirst = Math.Max(this.Thirst - item.RefreshmentValue, 0);

				return ActionState.Done;
			}
		}
	}
}
