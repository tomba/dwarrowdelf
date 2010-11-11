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
			job.StateChanged += OnJobStateChanged;
			GameData.Data.Jobs.Add(job);
		}

		void OnJobStateChanged(IJob job, JobState state)
		{
			switch (state)
			{
				case JobState.Done:
					job.StateChanged -= OnJobStateChanged;
					m_jobList.Remove(job);
					GameData.Data.Jobs.Remove(job);
					break;

				case JobState.Fail:
				case JobState.Abort:
				case JobState.Ok:
					break;
			}
		}

		bool IJobSource.HasWork
		{
			get { return m_jobList.Count > 0; }
		}

		IAssignment IJobSource.GetJob(ILiving living)
		{
			var jobs = m_jobList.Where(j => j.JobState == JobState.Ok);

			foreach (var job in jobs)
			{
				var assignment = JobManager.FindAssignment(job, living);

				if (assignment == null)
					continue;

				var jobState = assignment.Assign(living);

				switch (jobState)
				{
					case JobState.Ok:
						return assignment;

					case JobState.Done:
						throw new Exception();

					case JobState.Abort:
					case JobState.Fail:
						break;

					default:
						throw new Exception();
				}
			}

			return null;
		}
	}
}
