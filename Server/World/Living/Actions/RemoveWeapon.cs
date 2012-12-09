using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckRemoveWeapon(ItemObject item)
		{
			if (item == null)
			{
				var report = new RemoveWeaponActionReport(this, null);
				report.SetFail("object doesn't exist");
				SendReport(report);
				return false;
			}

			if (item.Parent != this)
			{
				var report = new RemoveWeaponActionReport(this, item);
				report.SetFail("doesn't have the object");
				SendReport(report);
				return false;
			}

			if (!item.IsWeapon)
			{
				var report = new RemoveWeaponActionReport(this, item);
				report.SetFail("not an weapon");
				SendReport(report);
				return false;
			}

			if (item.IsWielded == false)
			{
				var report = new RemoveWeaponActionReport(this, item);
				report.SetFail("not wielded");
				SendReport(report);
				return false;
			}

			return true;
		}

		ActionState ProcessAction(RemoveWeaponAction action)
		{
			if (this.ActionTicksUsed == 1)
			{
				var item = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID) as ItemObject;

				if (CheckRemoveWeapon(item) == false)
					return ActionState.Fail;

				this.ActionTotalTicks = 2;

				return ActionState.Ok;
			}
			else if (this.ActionTicksUsed < this.ActionTotalTicks)
			{
				return ActionState.Ok;
			}
			else
			{
				var item = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID) as ItemObject;

				if (CheckRemoveWeapon(item) == false)
					return ActionState.Fail;

				this.RemoveWeapon(item);

				var report = new RemoveWeaponActionReport(this, item);
				SendReport(report);

				return ActionState.Done;
			}
		}
	}
}
