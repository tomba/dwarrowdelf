using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(CarryItemAction action)
		{
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = 1;

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			var report = new CarryItemActionReport(this, item);

			if (item == null)
			{
				SendFailReport(report, "item not found");
				return ActionState.Fail;
			}

			if (this.CarriedItem != null)
			{
				SendFailReport(report, "already carrying an item");
				return ActionState.Fail;
			}

			if (item.Environment != this.Environment || item.Location != this.Location)
			{
				SendFailReport(report, "item not there");
				return ActionState.Fail;
			}

			if (item.MoveTo(this) == false)
			{
				SendFailReport(report, "failed to move");
				return ActionState.Fail;
			}

			this.CarriedItem = item;

			SendReport(report);

			return ActionState.Done;
		}
	}
}
