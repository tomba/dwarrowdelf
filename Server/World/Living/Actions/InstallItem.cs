using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(InstallItemAction action)
		{
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = 6;

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (item == null)
			{
				SendFailReport(new InstallItemActionReport(this, null, action.Mode), "item doesn't exists");
				return ActionState.Fail;
			}

			var report = new InstallItemActionReport(this, item, action.Mode);

			if (item.Environment != this.Environment || item.Location != this.Location)
			{
				SendFailReport(report, "item not here");
				return ActionState.Fail;
			}

			if (item.ItemInfo.IsInstallable == false)
			{
				SendFailReport(report, "item not installable");
				return ActionState.Fail;
			}

			switch (action.Mode)
			{
				case InstallMode.Install:

					if (item.IsInstalled)
					{
						SendFailReport(report, "item already installed");
						return ActionState.Fail;
					}

					item.IsInstalled = true;

					break;

				case InstallMode.Uninstall:

					if (!item.IsInstalled)
					{
						SendFailReport(report, "item not installed");
						return ActionState.Fail;
					}

					item.IsInstalled = false;

					break;

				default:
					throw new Exception();
			}

			SendReport(report);

			return ActionState.Done;
		}
	}
}
