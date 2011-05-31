﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	public class MoveToAreaAssignment : MoveAssignmentBase
	{
		readonly IntCuboid m_dest;

		public MoveToAreaAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntCuboid destination, DirectionSet positioning)
			: base(parent, priority, environment, positioning)
		{
			m_dest = destination;
		}

		protected override Queue<Direction> GetPath(ILiving worker)
		{
			var res = AStar.AStar.Find(m_environment, worker.Location, DirectionSet.Exact, new AStar.AStarAreaTarget(m_dest));

			if (res.Status != AStar.AStarStatus.Found)
				return null;

			var path = res.GetPath();

			return new Queue<Direction>(path);
		}

		protected override JobStatus CheckProgress(ILiving worker)
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