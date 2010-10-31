using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;

namespace Dwarrowdelf.Jobs
{
	public interface IJobSource
	{
		bool HasWork { get; }
		IEnumerable<IJob> GetJobs(ILiving living);
		void JobTaken(ILiving living, IJob job);
	}

	public class JobManager
	{
		List<IJobSource> m_jobSources = new List<IJobSource>();

		public JobManager(IWorld world)
		{
		}

		public void AddJobSource(IJobSource jobSource)
		{
			m_jobSources.Add(jobSource);
		}

		public void RemoveJobSource(IJobSource jobSource)
		{
			Debug.Assert(m_jobSources.Contains(jobSource));
			m_jobSources.Remove(jobSource);
		}

		public IAssignment FindJob(ILiving living)
		{
			foreach (var jobSource in m_jobSources.Where(js => js.HasWork))
			{
				var jobs = jobSource.GetJobs(living);
				foreach (var job in jobs)
				{
					var assignment = FindAssignment(job, living);
					if (assignment != null)
					{
						jobSource.JobTaken(living, job);
						return assignment;
					}
				}
			}

			return null;
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
