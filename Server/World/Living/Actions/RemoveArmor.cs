using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		bool CheckRemoveArmor(RemoveArmorAction action)
		{
			var itemID = action.ItemID;

			var item = this.World.FindObject<ItemObject>(itemID);

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

		int GetTotalTicks(RemoveArmorAction action)
		{
			if (CheckRemoveArmor(action) == false)
				return -1;

			return 10;
		}

		bool PerformAction(RemoveArmorAction action)
		{
			if (CheckRemoveArmor(action) == false)
				return false;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);

			this.RemoveArmor(item);

			var report = new RemoveArmorActionReport(this, item);
			SendReport(report);

			return true;
		}
	}
}
