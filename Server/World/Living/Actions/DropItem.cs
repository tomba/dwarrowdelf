using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckDropItemAction(ItemObject item)
		{
			if (item == null)
			{
				SendFailReport(new DropItemActionReport(this, item), "item not found");
				return false;
			}

			if (item.Container != this)
			{
				SendFailReport(new DropItemActionReport(this, item), "not in inventory");
				return false;
			}

			if (item.IsEquipped)
			{
				SendFailReport(new DropItemActionReport(this, item), "item equipped");
				return false;
			}

			return true;
		}

		ActionState ProcessAction(DropItemAction action)
		{
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = 1;

			if (this.Environment == null)
			{
				SendFailReport(new DropItemActionReport(this, null), "no environment");
				return ActionState.Fail;
			}

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (CheckDropItemAction(item) == false)
				return ActionState.Fail;

			if (item.MoveTo(this.Environment, this.Location) == false)
			{
				SendFailReport(new DropItemActionReport(this, item), "failed to move");
				return ActionState.Fail;
			}

			if (this.CarriedItem == item)
				this.CarriedItem = null;

			SendReport(new DropItemActionReport(this, item));

			return ActionState.Done;
		}
	}
}
