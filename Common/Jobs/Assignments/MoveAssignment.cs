using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	[GameObject(UseRef = true)]
	public class MoveAssignment : MoveAssignmentBase
	{
		[GameProperty("Dest")]
		readonly IntPoint3D m_dest;

		public MoveAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D destination, DirectionSet positioning)
			: base(parent, priority, environment, positioning)
		{
			m_dest = destination;
		}

		protected MoveAssignment(GameSerializationContext ctx)
			: base(ctx)
		{
		}

		protected override Queue<Direction> GetPath(ILiving worker)
		{
			IntPoint3D finalPos;
			var path = AStar.AStar.Find(m_environment, worker.Location, m_dest, this.Positioning, out finalPos);

			if (path == null)
				return null;

			return new Queue<Direction>(path);
		}

		protected override JobStatus CheckProgress(ILiving worker)
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
