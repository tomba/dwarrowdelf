using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckWearArmor(WearArmorAction action)
		{
			var itemID = action.ItemID;

			var item = this.World.FindObject<ItemObject>(itemID);

			if (item == null)
			{
				var report = new WearArmorActionReport(this, null);
				report.SetFail("object doesn't exist");
				SendReport(report);
				return false;
			}

			if (item.Parent != this)
			{
				var report = new WearArmorActionReport(this, item);
				report.SetFail("doesn't have the object");
				SendReport(report);
				return false;
			}

			if (!item.IsArmor)
			{
				var report = new WearArmorActionReport(this, item);
				report.SetFail("not an armor");
				SendReport(report);
				return false;
			}

			if (item.IsWorn)
			{
				var report = new WearArmorActionReport(this, item);
				report.SetFail("already worn");
				SendReport(report);
				return false;
			}

			return true;
		}

		int GetTotalTicks(WearArmorAction action)
		{
			if (CheckWearArmor(action) == false)
				return -1;

			return 10;
		}

		bool PerformAction(WearArmorAction action)
		{
			if (CheckWearArmor(action) == false)
				return false;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);

			this.WearArmor(item);

			var report = new WearArmorActionReport(this, item);
			SendReport(report);

			return true;
		}


	}
}
