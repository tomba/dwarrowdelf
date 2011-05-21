﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[GameObject(UseRef = true)]
	public class FellTreeAssignment : Assignment
	{
		[GameProperty]
		readonly IntPoint3D m_location;
		[GameProperty]
		readonly IEnvironment m_environment;

		public FellTreeAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location)
			: base(parent, priority)
		{
			m_environment = environment;
			m_location = location;
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var v = m_location - this.Worker.Location;

			if (!this.Worker.Location.IsAdjacentTo(m_location, DirectionSet.Planar))
			{
				progress = JobStatus.Fail;
				return null;
			}

			if (CheckProgress() == JobStatus.Done)
			{
				progress = JobStatus.Done;
				return null;
			}

			var action = new FellTreeAction(v.ToDirection(), this.Priority);
			progress = JobStatus.Ok;
			return action;
		}

		protected override JobStatus ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return JobStatus.Ok;

				case ActionState.Done:
					return CheckProgress();

				case ActionState.Fail:
					return JobStatus.Fail;

				case ActionState.Abort:
					return JobStatus.Abort;

				default:
					throw new Exception();
			}
		}

		JobStatus CheckProgress()
		{
			if (m_environment.GetInterior(m_location).ID == InteriorID.Empty)
				return JobStatus.Done;
			else
				return JobStatus.Ok;
		}

		public override string ToString()
		{
			return "FellTree";
		}
	}
}
