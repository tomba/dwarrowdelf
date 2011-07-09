using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class FellTreeParallelJob : JobGroup
	{
		readonly IEnvironment m_environment;
		readonly IntCuboid m_area;

		IEnumerable<IntPoint3D> m_locs;

		List<Tuple<IntPoint3D, IJob>> m_jobs = new List<Tuple<IntPoint3D, IJob>>();

		public FellTreeParallelJob(IEnvironment env, ActionPriority priority, IntCuboid area)
			: base(null, priority)
		{
			m_environment = env;
			m_area = area;

			AddNewJobs();
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);
			Debug.Assert(m_jobs.FindIndex(i => i.Item2 == job) != -1);
			m_jobs.RemoveAt(m_jobs.FindIndex(i => i.Item2 == job));

			AddNewJobs();

			if (this.SubJobs.Count == 0)
				SetStatus(JobStatus.Done);
		}

		void AddNewJobs()
		{
			var c = this.SubJobs.Count;

			m_locs = m_area.Range().Where(p => !m_jobs.Any(i => i.Item1 == p) && m_environment.GetInterior(p).ID == InteriorID.Tree).Take(3 - c);

			foreach (var p in m_locs)
			{
				var job = new AssignmentGroups.MoveFellTreeJob(this, this.Priority, m_environment, p);
				AddSubJob(job);
				m_jobs.Add(new Tuple<IntPoint3D, IJob>(p, job));
			}

		}

		public override string ToString()
		{
			return "FellTreeParallelJob";
		}
	}
}
