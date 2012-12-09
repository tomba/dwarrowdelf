using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckWieldWeapon(ItemObject item)
		{
			if (item == null)
			{
				var report = new WieldWeaponActionReport(this, null);
				report.SetFail("object doesn't exist");
				SendReport(report);
				return false;
			}

			if (item.Parent != this)
			{
				var report = new WieldWeaponActionReport(this, item);
				report.SetFail("doesn't have the object");
				SendReport(report);
				return false;
			}

			if (!item.IsWeapon)
			{
				var report = new WieldWeaponActionReport(this, item);
				report.SetFail("not a weapon");
				SendReport(report);
				return false;
			}

			if (item.IsWielded)
			{
				var report = new WieldWeaponActionReport(this, item);
				report.SetFail("already wielded");
				SendReport(report);
				return false;
			}

			return true;
		}

		ActionState ProcessAction(WieldWeaponAction action)
		{
			if (this.ActionTicksUsed == 1)
			{
				var item = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID) as ItemObject;

				if (CheckWieldWeapon(item) == false)
					return ActionState.Fail;

				this.ActionTotalTicks = 3;

				return ActionState.Ok;
			}
			else if (this.ActionTicksUsed < this.ActionTotalTicks)
			{
				return ActionState.Ok;
			}
			else
			{
				var item = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID) as ItemObject;

				if (CheckWieldWeapon(item) == false)
					return ActionState.Fail;

				this.WieldWeapon(item);

				var report = new WieldWeaponActionReport(this, item);
				SendReport(report);

				return ActionState.Done;
			}
		}
	}
}
