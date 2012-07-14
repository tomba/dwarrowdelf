using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{

		bool CheckWieldWeapon(WieldWeaponAction action)
		{
			var itemID = action.ItemID;

			var item = this.World.FindObject<ItemObject>(itemID);

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

		int GetTotalTicks(WieldWeaponAction action)
		{
			if (CheckWieldWeapon(action) == false)
				return -1;

			return 3;
		}

		bool PerformAction(WieldWeaponAction action)
		{
			if (CheckWieldWeapon(action) == false)
				return false;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);

			this.WieldWeapon(item);

			var report = new WieldWeaponActionReport(this, item);
			SendReport(report);

			return true;
		}

	}
}
