using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	[SaveGameObject]
	public sealed class PatrolAssignment : AssignmentGroup
	{
		[SaveGameProperty("Environment")]
		readonly IEnvironmentObject m_environment;
		[SaveGameProperty("State")]
		int m_state;

		[SaveGameProperty]
		IntVector3[] m_waypoints;

		public PatrolAssignment(IJobObserver parent, IEnvironmentObject environment, IntVector3[] waypoints)
			: base(parent)
		{
			m_environment = environment;
			m_waypoints = waypoints;
		}

		PatrolAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void AssignOverride(ILivingObject worker)
		{
			// start at waypoint closest to the worker
			m_state = m_waypoints
				.Select((p, i) => new Tuple<IntVector3, int>(p, i))
				.OrderBy(t => (t.Item1 - worker.Location).Length).First().Item2;
		}

		protected override void OnAssignmentDone()
		{
			m_state = (m_state + 1) % m_waypoints.Length;
		}

		protected override IAssignment PrepareNextAssignment()
		{
			return new MoveAssignment(this, m_environment, m_waypoints[m_state], DirectionSet.Exact);
		}

		public override string ToString()
		{
			return "PatrolAssignment";
		}
	}
}
