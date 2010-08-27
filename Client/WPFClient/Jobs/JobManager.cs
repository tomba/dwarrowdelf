using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MyGame.Client
{
	class JobManager
	{
		ObservableCollection<IJob> m_jobs;
		public ReadOnlyObservableCollection<IJob> Jobs { get; private set; }

		World m_world;

		public JobManager(World world)
		{
			m_world = world;

			m_jobs = new ObservableCollection<IJob>();
			this.Jobs = new ReadOnlyObservableCollection<IJob>(m_jobs);

			m_world.TickIncreased += OnTickIncreased;
		}

		void OnTickIncreased()
		{
			var doneJobs = m_jobs.Where(j => j.Progress == Progress.Done).ToArray();
			foreach (var job in doneJobs)
				m_jobs.Remove(job);
		}

		public void Add(IJob job)
		{
			Debug.Assert(job.Parent == null);
			m_jobs.Add(job);
		}

		public void Remove(IJob job)
		{
			Debug.Assert(job.Parent == null);
			job.Abort();
			m_jobs.Remove(job);
		}

		public IActionJob FindAndAssignJob(ILiving living)
		{
			return FindAndAssignJob(m_jobs, living);
		}

		static IActionJob FindAndAssignJob(IEnumerable<IJob> jobs, ILiving living)
		{
			while (true)
			{
				var job = FindJob(jobs);

				if (job == null)
					return null;

				var progress = job.Assign(living);

				switch (progress)
				{
					case Progress.Ok:
						return job;

					case Progress.Done:
						break;

					case Progress.Fail:
						break;

					case Progress.Abort:
						break;

					case Progress.None:
					default:
						throw new Exception();
				}
			}
		}

		static IActionJob FindJob(IEnumerable<IJob> jobs)
		{
			return FindJob(jobs, JobGroupType.Parallel);
		}

		static IActionJob FindJob(IEnumerable<IJob> jobs, JobGroupType type)
		{
			if (type != JobGroupType.Parallel && type != JobGroupType.Serial)
				throw new Exception();

			foreach (var job in jobs)
			{
				if (job.Progress == Progress.Done)
					continue;

				if (job.Progress == Progress.None || job.Progress == Progress.Abort)
				{
					// job can be taken

					if (job is IActionJob)
					{
						var ajob = (IActionJob)job;
						return ajob;
					}
					else if (job is IJobGroup)
					{
						var gjob = (IJobGroup)job;

						var j = FindJob(gjob.SubJobs, gjob.JobGroupType);

						if (j != null)
							return j;
					}
					else
					{
						throw new Exception();
					}
				}

				// job cannot be taken

				if (type == JobGroupType.Serial)
					return null;
			}

			return null;

		}

	}
}
