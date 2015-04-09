using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckUnequipItem(ItemObject item)
		{
			if (item == null)
			{
				var report = new UnequipItemActionReport(this, null);
				report.SetFail("object doesn't exist");
				SendReport(report);
				return false;
			}

			if (item.Parent != this)
			{
				var report = new UnequipItemActionReport(this, item);
				report.SetFail("doesn't have the object");
				SendReport(report);
				return false;
			}

			if (!item.IsArmor && !item.IsWeapon)
			{
				var report = new UnequipItemActionReport(this, item);
				report.SetFail("not equippable");
				SendReport(report);
				return false;
			}

			if (item.IsEquipped == false)
			{
				var report = new UnequipItemActionReport(this, item);
				report.SetFail("not equipped");
				SendReport(report);
				return false;
			}

			return true;
		}

		ActionState ProcessAction(UnequipItemAction action)
		{
			if (this.ActionTicksUsed == 1)
			{
				var item = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID) as ItemObject;

				if (CheckUnequipItem(item) == false)
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

				if (CheckUnequipItem(item) == false)
					return ActionState.Fail;

				this.UnequipItem(item);

				var report = new UnequipItemActionReport(this, item);
				SendReport(report);

				return ActionState.Done;
			}
		}
	}
}
