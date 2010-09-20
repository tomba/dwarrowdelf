using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.ActionJobs;

namespace Dwarrowdelf.Jobs.SerialActionJobs
{
	public class MineAreaJob : StaticSerialActionJob
	{
		readonly IEnvironment m_environment;
		readonly IntRect m_rect;

		IEnumerable<IntPoint> m_locs;

		public MineAreaJob(IEnvironment env, ActionPriority priority, IntRect rect, int z)
			: base(null, priority)
		{
			m_environment = env;
			m_rect = rect;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.Wall);

			List<IActionJob> jobs = new List<IActionJob>();
			foreach (var p in m_locs)
			{
				var job = new MoveMineJob(this, priority, env, new IntPoint3D(p, z));
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
			return "MineAreaSerialSameJob";
		}
	}
}
