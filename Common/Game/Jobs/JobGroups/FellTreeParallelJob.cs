using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public sealed class FellTreeParallelJob : JobGroup
	{
		readonly IEnvironmentObject m_environment;
		readonly IntGrid3 m_area;

		IEnumerable<IntVector3> m_locs;

		List<Tuple<IntVector3, IJob>> m_jobs = new List<Tuple<IntVector3, IJob>>();

		public FellTreeParallelJob(IEnvironmentObject env, IntGrid3 area)
			: base(null)
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

			m_locs = m_area.Range().Where(p => !m_jobs.Any(i => i.Item1 == p) && m_environment.GetInteriorID(p) == InteriorID.Tree).Take(3 - c);

			foreach (var p in m_locs)
			{
				var job = new AssignmentGroups.MoveFellTreeAssignment(this, m_environment, p);
				AddSubJob(job);
				m_jobs.Add(new Tuple<IntVector3, IJob>(p, job));
			}

		}

		public override string ToString()
		{
			return "FellTreeParallelJob";
		}
	}
}
