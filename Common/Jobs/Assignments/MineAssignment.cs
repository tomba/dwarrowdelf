using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	public class MineAssignment : Assignment
	{
		readonly IntPoint3D m_location;
		readonly MineActionType m_mineActionType;
		readonly IEnvironment m_environment;

		public MineAssignment(IJob job, ActionPriority priority, IEnvironment environment, IntPoint3D location, MineActionType mineActionType)
			: base(job, priority)
		{
			m_environment = environment;
			m_location = location;
			m_mineActionType = mineActionType;
		}

		protected override GameAction PrepareNextActionOverride(out JobState progress)
		{
			var v = m_location - this.Worker.Location;

			if (!this.Worker.Location.IsAdjacentTo(m_location, DirectionSet.PlanarUpDown))
			{
				progress = JobState.Fail;
				return null;
			}

			if (CheckProgress() == JobState.Done)
			{
				progress = JobState.Done;
				return null;
			}

			var action = new MineAction(v.ToDirection(), m_mineActionType, this.Priority);
			progress = JobState.Ok;
			return action;
		}

		protected override JobState ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return JobState.Ok;

				case ActionState.Done:
					return CheckProgress();

				case ActionState.Fail:
					return JobState.Fail;

				case ActionState.Abort:
					return JobState.Abort;

				default:
					throw new Exception();
			}
		}

		JobState CheckProgress()
		{
			var intID = m_environment.GetInterior(m_location).ID;

			if (intID != InteriorID.NaturalWall && intID != InteriorID.Undefined) // XXX
				return JobState.Done;
			else
				return JobState.Ok;
		}

		public override string ToString()
		{
			return "Mine";
		}
	}
}
