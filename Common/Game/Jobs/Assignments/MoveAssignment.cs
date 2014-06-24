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
	public sealed class MoveAssignment : MoveAssignmentBase
	{
		[SaveGameProperty("Dest")]
		readonly IntVector3 m_dest;

		public MoveAssignment(IJobObserver parent, IEnvironmentObject environment, IntVector3 destination, DirectionSet positioning)
			: base(parent, environment, positioning)
		{
			m_dest = destination;
		}

		MoveAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override Queue<Direction> GetPath(ILivingObject worker)
		{
			var path = AStar.Find(m_environment, worker.Location, m_dest, this.Positioning);

			if (path == null)
				return null;

			return new Queue<Direction>(path);
		}

		protected override JobStatus CheckProgress(ILivingObject worker)
		{
			if (worker.Location.IsAdjacentTo(m_dest, this.Positioning))
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
