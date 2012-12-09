using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckRemoveArmor(ItemObject item)
		{
			if (item == null)
			{
				var report = new RemoveArmorActionReport(this, null);
				report.SetFail("object doesn't exist");
				SendReport(report);
				return false;
			}

			if (item.Parent != this)
			{
				var report = new RemoveArmorActionReport(this, item);
				report.SetFail("doesn't have the object");
				SendReport(report);
				return false;
			}

			if (!item.IsArmor)
			{
				var report = new RemoveArmorActionReport(this, item);
				report.SetFail("not an armor");
				SendReport(report);
				return false;
			}

			if (item.IsWorn == false)
			{
				var report = new RemoveArmorActionReport(this, item);
				report.SetFail("not worn");
				SendReport(report);
				return false;
			}

			return true;
		}

		ActionState ProcessAction(RemoveArmorAction action)
		{
			if (this.ActionTicksUsed == 1)
			{
				var item = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID) as ItemObject;

				if (CheckRemoveArmor(item) == false)
					return ActionState.Fail;

				this.ActionTotalTicks = 10;

				return ActionState.Ok;
			}
			else if (this.ActionTicksUsed < this.ActionTotalTicks)
			{
				return ActionState.Ok;
			}
			else
			{
				var item = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID) as ItemObject;

				if (CheckRemoveArmor(item) == false)
					return ActionState.Fail;

				this.RemoveArmor(item);

				var report = new RemoveArmorActionReport(this, item);
				SendReport(report);

				return ActionState.Done;
			}
		}
	}
}
