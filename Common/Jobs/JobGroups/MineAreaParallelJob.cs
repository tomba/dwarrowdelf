using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class MineAreaParallelJob : ParallelJobGroup
	{
		readonly IEnvironment m_environment;
		readonly IntRect m_rect;

		IEnumerable<IntPoint> m_locs;

		public MineAreaParallelJob(IEnvironment env, ActionPriority priority, IntRect rect, int z)
			: base(null, priority)
		{
			m_environment = env;
			m_rect = rect;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.Wall);

			foreach (var p in m_locs)
			{
				var job = new AssignmentGroups.MoveMineJob(this, priority, env, new IntPoint3D(p, z));
				AddSubJob(job);
			}

			env.World.TickEvent += World_TickEvent;
		}

		void World_TickEvent()
		{
			foreach (var job in this.SubJobs.Where(j => j.Progress == Jobs.Progress.Abort))
				job.Retry();
		}

		protected override void Cleanup()
		{
			m_environment.World.TickEvent -= World_TickEvent;
			m_locs = null;
		}

		public override string ToString()
		{
			return "MineAreaParallelJob";
		}
	}
}
