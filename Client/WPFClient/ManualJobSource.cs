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

		IEnumerable<IJob> IJobSource.GetJobs(ILiving living)
		{
			return m_jobList.Where(j => j.JobStatus == JobStatus.Ok);
		}
	}
}
