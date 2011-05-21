using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class FellTreeParallelJob : ParallelJobGroup
	{
		readonly IEnvironment m_environment;
		readonly IntCuboid m_area;

		IEnumerable<IntPoint3D> m_locs;

		public FellTreeParallelJob(IEnvironment env, ActionPriority priority, IntCuboid area)
			: base(null, priority)
		{
			m_environment = env;
			m_area = area;

			var jobs = new List<IJob>();
			m_locs = area.Range().Where(p => env.GetInterior(p).ID == InteriorID.Tree);
			foreach (var p in m_locs)
			{
				var job = new AssignmentGroups.MoveFellTreeJob(this, priority, env, p);
				jobs.Add(job);
			}
			SetSubJobs(jobs);

			env.World.TickStartEvent += World_TickEvent;
		}

		void World_TickEvent()
		{
			foreach (var job in this.SubJobs.Where(j => j.JobStatus == Jobs.JobStatus.Abort))
				job.Retry();
		}

		protected override void Cleanup()
		{
			m_environment.World.TickStartEvent -= World_TickEvent;
			m_locs = null;
		}

		public override string ToString()
		{
			return "FellTreeParallelJob";
		}
	}
}
