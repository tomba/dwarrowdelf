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
		IAssignment FindAssignment(ILivingObject living);
	}

	public sealed class JobManager
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

		public IAssignment FindAssignment(ILivingObject living)
		{
			foreach (var jobSource in m_jobSources)
			{
				var assignment = jobSource.FindAssignment(living);
				if (assignment != null)
					return assignment;
			}

			return null;
		}
	}
}
