using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame.Client
{
	public enum Progress
	{
		/// <summary>
		/// None
		/// </summary>
		None,
		/// <summary>
		/// Everything ok
		/// </summary>
		Ok,
		/// <summary>
		/// Job failed, and nobody else can do it either
		/// </summary>
		Fail,
		/// <summary>
		/// Job failed, the worker wasn't able to do it
		/// </summary>
		Abort,
		/// <summary>
		/// Job has been done successfully
		/// </summary>
		Done,
	}

	class AI
	{
		Living m_living;
		IActionJob m_currentJob;

		public AI(Living living)
		{
			m_living = living;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			MyDebug.WriteLine("[AI] [{0}]: {1}", m_living, String.Format(format, args));
		}

		public void ActionRequired()
		{
			//if (m_living == GameData.Data.CurrentObject)
			//	return;
			while (true)
			{
				if (m_currentJob == null)
				{
					m_currentJob = FindAndAssignJob(m_living.World.Jobs, m_living);

					if (m_currentJob == null)
					{
						D("no job to do");
						var action = new WaitAction(1);
						m_living.EnqueueAction(action);
						return;
					}
				}

				var progress = m_currentJob.PrepareNextAction();

				switch (progress)
				{
					case Progress.Ok:
						var action = m_currentJob.CurrentAction;
						if (action == null)
							throw new Exception();

						m_living.EnqueueAction(action);
						return;

					case Progress.Done:
					case Progress.Fail:
					case Progress.Abort:
						D("ActionRequired: {0} in {1}, looking for new job", progress, m_currentJob);
						m_currentJob = null;
						break;

					case Progress.None:
					default:
						throw new Exception();
				}
			}
		}

		public void ActionProgress(ActionProgressEvent e)
		{
			if (m_currentJob == null)
				return;

			var progress = m_currentJob.ActionProgress(e);

			switch (progress)
			{
				case Progress.None:
					throw new Exception();
				// break;

				case Progress.Ok:
					D("Job progressing");
					break;

				case Progress.Done:
				case Progress.Fail:
				case Progress.Abort:
					D("ActionProgress: {0} in {1}", progress, m_currentJob);
					m_currentJob = null;
					break;

				default:
					throw new Exception();
			}
		}

		static IActionJob FindAndAssignJob(IEnumerable<IJob> jobs, Living living)
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
