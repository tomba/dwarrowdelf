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
	public sealed class HaulToAreaAssignment : MoveAssignmentBase
	{
		[SaveGameProperty("Dest")]
		readonly IntGrid3 m_dest;

		public HaulToAreaAssignment(IJobObserver parent, IEnvironmentObject environment, IntGrid3 destination, DirectionSet positioning, IItemObject hauledItem)
			: base(parent, environment, positioning, hauledItem)
		{
			m_dest = destination;
		}

		HaulToAreaAssignment(SaveGameContext ctx)
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
			return String.Format("Haul({0} -> {1}, {2})", this.Src, m_dest, this.HauledItem);
		}
	}
}
