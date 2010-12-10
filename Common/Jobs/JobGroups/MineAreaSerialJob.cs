using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class MineAreaSerialJob : SerialJobGroup
	{
		readonly IEnvironment m_environment;
		readonly IntCuboid m_area;

		IEnumerable<IntPoint3D> m_locs;

		public MineAreaSerialJob(IEnvironment env, ActionPriority priority, IntCuboid area, MineActionType mineActionType)
			: base(null, priority)
		{
			m_environment = env;
			m_area = area;

			var jobs = new List<IJob>();
			m_locs = area.Range().Where(p => env.GetInterior(p).IsMineable);
			foreach (var p in m_locs)
			{
				var job = new AssignmentGroups.MoveMineJob(this, priority, env, p, mineActionType);
				jobs.Add(job);
			}
			SetSubJobs(jobs);
		}

		protected override void Cleanup()
		{
			m_locs = null;
		}

		public override string ToString()
		{
			return "MineAreaSerialJob";
		}
	}

}
