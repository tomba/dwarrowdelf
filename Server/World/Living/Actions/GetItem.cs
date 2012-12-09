using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckGetItemAction(ItemObject item)
		{
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

			return true;
		}

		ActionState ProcessAction(GetItemAction action)
		{
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = 1;

			if (this.Environment == null)
			{
				SendFailReport(new GetItemActionReport(this, null), "no environment");
				return ActionState.Fail;
			}

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (CheckGetItemAction(item) == false)
				return ActionState.Fail;

			if (item.MoveTo(this) == false)
			{
				SendFailReport(new GetItemActionReport(this, item), "failed to move");
				return ActionState.Fail;
			}

			SendReport(new GetItemActionReport(this, item));

			return ActionState.Done;
		}
	}
}
