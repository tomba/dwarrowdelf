using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject]
	public sealed class MoveToAreaAssignment : MoveAssignmentBase
	{
		[SaveGameProperty("Dest")]
		readonly IntGrid3 m_dest;

		public MoveToAreaAssignment(IJobObserver parent, IEnvironmentObject environment, IntGrid3 destination, DirectionSet positioning)
			: base(parent, environment, positioning)
		{
			m_dest = destination;
		}

		MoveToAreaAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override Queue<Direction> GetPath(ILivingObject worker)
		{
			var res = AStar.Find(m_environment, worker.Location, DirectionSet.Exact, new AStarAreaTarget(m_dest));

			if (res.Status != AStarStatus.Found)
				return null;

			var path = res.GetPath();

			return new Queue<Direction>(path);
		}

		protected override JobStatus CheckProgress(ILivingObject worker)
		{
			if (m_dest.Contains(worker.Location))
				return JobStatus.Done;
			else
				return JobStatus.Ok;
		}

		public override string ToString()
		{
			return String.Format("Move({0} -> {1})", this.Src, m_dest);
		}
	}
}
