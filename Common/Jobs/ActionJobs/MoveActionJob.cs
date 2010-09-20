using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.ActionJobs
{
	public class MoveActionJob : ActionJob
	{
		IntPoint3D m_src; // just for ToString()

		Queue<Direction> m_pathDirs;
		readonly IEnvironment m_environment;
		readonly IntPoint3D m_dest;
		readonly bool m_adjacent;
		IntPoint3D m_supposedLocation;
		int m_numFails;

		public MoveActionJob(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D destination, bool adjacent)
			: base(parent, priority)
		{
			m_environment = environment;
			m_dest = destination;
			m_adjacent = adjacent;
		}

		protected override void Cleanup()
		{
			m_pathDirs = null;
		}

		protected override void AbortOverride()
		{
			m_pathDirs = null;
			m_numFails = 0;
		}

		protected override Progress AssignOverride(ILiving worker)
		{
			m_src = worker.Location;

			m_numFails = 0;
			return Progress.Ok;
		}

		protected override Progress PrepareNextActionOverride()
		{
			if (m_pathDirs == null || m_supposedLocation != this.Worker.Location)
			{
				var res = PreparePath();

				if (res != Progress.Ok)
					return res;
			}

			Direction dir = m_pathDirs.Dequeue();

			if (m_pathDirs.Count == 0)
				m_pathDirs = null;

			var action = new MoveAction(dir, this.Priority);
			m_supposedLocation += new IntVector3D(dir);

			this.CurrentAction = action;

			return Progress.Ok;
		}

		protected override Progress ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return Progress.Ok;

				case ActionState.Done:
					return CheckProgress();

				case ActionState.Fail:
					m_numFails++;
					if (m_numFails > 10)
						return Progress.Fail;

					var res = PreparePath();
					return res;

				case ActionState.Abort:
					return Progress.Abort;

				default:
					throw new Exception();
			}
		}

		Progress PreparePath()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
			{
				m_pathDirs = null;
				return Progress.Done;
			}

			var res = AStar.AStar3D.Find(this.Worker.Location, m_dest, !m_adjacent, l => 0, m_environment.GetDirectionsFrom);
			var dirs = res.GetPath();

			m_pathDirs = new Queue<Direction>(dirs);

			if (m_pathDirs.Count == 0)
			{
				m_pathDirs = null;
				return Progress.Fail;
			}

			m_supposedLocation = this.Worker.Location;

			return Progress.Ok;
		}

		Progress CheckProgress()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
				return Progress.Done;
			else
				return Progress.Ok;
		}

		public override string ToString()
		{
			return String.Format("Move({0} -> {1})", m_src, m_dest);
		}

	}
}
