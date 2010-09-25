﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Jobs
{
	public interface IAI
	{
		/// <summary>
		/// In server this is called two times per turn, once for high priority and once for idle priority.
		/// In client this is called once per turn, if the living doesn't have an action or the action is lower than high priority.
		/// </summary>
		/// <param name="priority"></param>
		/// <returns>Action to do, possibly overriding the current action, or null to continue doing the current action</returns>
		GameAction DecideAction(ActionPriority priority);

		/// <summary>
		/// Called when worker starts a new action
		/// Note: can be an action started by something else than this AI
		/// </summary>
		void ActionStarted(ActionStartedChange change);

		/// <summary>
		/// Called when worker's current action's state changes.
		/// Note: can be an action started by something else than this AI
		/// </summary>
		/// <param name="e"></param>
		void ActionProgress(ActionProgressChange change);
	}
}