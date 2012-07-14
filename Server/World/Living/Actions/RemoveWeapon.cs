using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckRemoveWeapon(RemoveWeaponAction action)
		{
			var itemID = action.ItemID;

			var item = this.World.FindObject<ItemObject>(itemID);

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

		int GetTotalTicks(RemoveWeaponAction action)
		{
			if (CheckRemoveWeapon(action) == false)
				return -1;

			return 2;
		}

		bool PerformAction(RemoveWeaponAction action)
		{
			if (CheckRemoveWeapon(action) == false)
				return false;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);

			this.RemoveWeapon(item);

			var report = new RemoveWeaponActionReport(this, item);
			SendReport(report);

			return true;
		}
	}
}
