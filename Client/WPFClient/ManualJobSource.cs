﻿using System;
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

		IEnumerable<IJob> IJobSource.GetJobs(ILiving living)
		{
			return m_jobList.Where(j => j.JobState == JobState.Ok);
		}

		void IJobSource.JobTaken(ILiving living, IJob job)
		{
		}
	}
}