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

		public IAssignment FindAssignment(ILiving living)
		{
			foreach (var jobSource in m_jobSources.Where(js => js.HasWork))
			{
				foreach (var job in jobSource.GetJobs(living))
				{
					foreach (var assignment in job.GetAssignments(living))
					{
						var jobState = assignment.Assign(living);

						switch (jobState)
						{
							case JobStatus.Ok:
								return assignment;

							case JobStatus.Done:
								throw new Exception();

							case JobStatus.Abort:
							case JobStatus.Fail:
								throw new Exception();

							default:
								throw new Exception();
						}
					}
				}
			}

			return null;
		}
	}
}
