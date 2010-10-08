using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;

namespace Dwarrowdelf.Jobs
{
	public class JobManager
	{
		ObservableCollection<IJob> m_jobs;
		public ReadOnlyObservableCollection<IJob> Jobs { get; private set; }

		public JobManager(IWorld world)
		{
			m_jobs = new ObservableCollection<IJob>();
			this.Jobs = new ReadOnlyObservableCollection<IJob>(m_jobs);
		}

		public void Add(IJob job)
		{
			Debug.Assert(job.Parent == null);
			m_jobs.Add(job);
		}

		public void Remove(IJob job)
		{
			Debug.Assert(job.Parent == null);
			if (job.JobState == JobState.Ok)
				job.Abort();
			m_jobs.Remove(job);
		}

		public IAssignment FindJob(ILiving living)
		{
			return FindJob(m_jobs, living);
		}

		static IAssignment FindJob(IEnumerable<IJob> jobs, ILiving living)
		{
			return FindJobParallel(jobs, living);
		}

		static IAssignment FindJobParallel(IEnumerable<IJob> jobs, ILiving living)
		{
			foreach (var job in jobs.Where(j => j.JobState == JobState.Ok))
			{
				var assignment = FindAssignment(job, living);

				if (assignment != null)
					return assignment;
			}

			return null;
		}

		static IAssignment FindJobSerial(IEnumerable<IJob> jobs, ILiving living)
		{
			Debug.Assert(jobs.All(j => j.JobState == JobState.Ok || j.JobState == JobState.Done));

			var job = jobs.First(j => j.JobState == JobState.Ok);

			var assigment = FindAssignment(job, living);

			return assigment;
		}

		static IAssignment FindAssignment(IJob job, ILiving living)
		{
			Debug.Assert(job.JobState == JobState.Ok);

			IAssignment assignment;

			switch (job.JobType)
			{
				case JobType.Assignment:
					assignment = (IAssignment)job;
					break;

				case JobType.JobGroup:
					var jobGroup = (IJobGroup)job;

					switch (jobGroup.JobGroupType)
					{
						case JobGroupType.Parallel:
							assignment = FindJobParallel(jobGroup.SubJobs, living);
							break;

						case JobGroupType.Serial:
							assignment = FindJobSerial(jobGroup.SubJobs, living);
							break;

						default:
							throw new Exception();
					}
					break;

				default:
					throw new Exception();
			}

			if (assignment != null && assignment.IsAssigned == false)
				return assignment;
			else
				return null;
		}
	}
}
