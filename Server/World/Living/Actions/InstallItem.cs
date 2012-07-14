using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(InstallItemAction action)
		{
			return 6;
		}

		bool PerformAction(InstallItemAction action)
		{
			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (item == null)
			{
				SendFailReport(new InstallItemActionReport(this, null, action.Mode), "item doesn't exists");
				return false;
			}

			var report = new InstallItemActionReport(this, item, action.Mode);

			if (item.Environment != this.Environment || item.Location != this.Location)
			{
				SendFailReport(report, "item not here");
				return false;
			}

			switch (action.Mode)
			{
				case InstallMode.Install:

					if (item.IsInstalled)
					{
						SendFailReport(report, "item already installed");
						return false;
					}

					item.IsInstalled = true;

					break;

				case InstallMode.Uninstall:

					if (!item.IsInstalled)
					{
						SendFailReport(report, "item not installed");
						return false;
					}

					item.IsInstalled = false;

					break;

				default:
					throw new Exception();
			}

			SendReport(report);

			return true;
		}

	}
}
