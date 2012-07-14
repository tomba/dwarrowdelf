using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(WaitAction action)
		{
			return action.WaitTicks;
		}

		bool PerformAction(WaitAction action)
		{
			return true;
		}
	}
}
