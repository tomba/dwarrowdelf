using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	class ManualJobSource : IJobSource
	{
		List<IJob> m_jobList;

		public ManualJobSource(JobManager jobManager)
		{
			m_jobList = new List<IJob>();
			jobManager.AddJobSource(this); // XXX not removed
		}

		public void Add(IJob job)
		{
			m_jobList.Add(job);
			job.StatusChanged += OnJobStatusChanged;
			GameData.Data.Jobs.Add(job);
		}

		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			switch (status)
			{
				case JobStatus.Done:
					job.StatusChanged -= OnJobStatusChanged;
					m_jobList.Remove(job);
					GameData.Data.Jobs.Remove(job);
					break;

				case JobStatus.Fail:
				case JobStatus.Abort:
				case JobStatus.Ok:
					break;
			}
		}

		bool IJobSource.HasWork
		{
			get { return m_jobList.Count > 0; }
		}

		IAssignment IJobSource.GetJob(ILiving living)
		{
			var jobs = m_jobList.Where(j => j.JobStatus == JobStatus.Ok);

			foreach (var job in jobs)
			{
				var assignment = JobManager.FindAssignment(job, living);

				if (assignment == null)
					continue;

				var jobState = assignment.Assign(living);

				switch (jobState)
				{
					case JobStatus.Ok:
						return assignment;

					case JobStatus.Done:
						throw new Exception();

					case JobStatus.Abort:
					case JobStatus.Fail:
						break;

					default:
						throw new Exception();
				}
			}

			return null;
		}
	}
}
