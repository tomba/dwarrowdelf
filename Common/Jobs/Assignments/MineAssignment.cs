using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject(UseRef = true)]
	public class MineAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IntPoint3D m_location;
		[SaveGameProperty]
		readonly MineActionType m_mineActionType;
		[SaveGameProperty]
		readonly IEnvironment m_environment;

		public MineAssignment(IJob job, ActionPriority priority, IEnvironment environment, IntPoint3D location, MineActionType mineActionType)
			: base(job, priority)
		{
			m_environment = environment;
			m_location = location;
			m_mineActionType = mineActionType;
		}

		protected MineAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var v = m_location - this.Worker.Location;

			if (!this.Worker.Location.IsAdjacentTo(m_location, DirectionSet.PlanarUpDown))
			{
				progress = JobStatus.Fail;
				return null;
			}

			if (CheckProgress() == JobStatus.Done)
			{
				progress = JobStatus.Done;
				return null;
			}

			var action = new MineAction(v.ToDirection(), m_mineActionType, this.Priority);
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
			var inter = m_environment.GetInterior(m_location);

			if (inter.ID == InteriorID.Undefined || inter.IsMineable)
				return JobStatus.Ok;
			else
				return JobStatus.Done;
		}

		public override string ToString()
		{
			return "Mine";
		}
	}
}
