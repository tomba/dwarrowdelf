using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		IJob m_currentJob;

		public AI(Living living)
		{
			m_living = living;
		}

		void HandleProgress(Progress progress)
		{
			switch (progress)
			{
				case Progress.None:
					break;

				case Progress.Ok:
					MyDebug.WriteLine("[AI] Job progressing");
					break;

				case Progress.Done:
					MyDebug.WriteLine("[AI] JOB DONE ({0})!", m_currentJob);
					World.TheWorld.Jobs.Remove(m_currentJob);
					m_currentJob = null;
					break;

				case Progress.Fail:
					MyDebug.WriteLine("[AI] JOB FAIL ({0})!!!", m_currentJob);
					World.TheWorld.Jobs.Remove(m_currentJob);
					m_currentJob = null;
					break;

				case Progress.Abort:
					MyDebug.WriteLine("[AI] JOB ABORT ({0})!!!", m_currentJob);
					m_currentJob = null;
					break;

				default:
					throw new Exception();
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
					break;

				case Progress.Ok:
					MyDebug.WriteLine("[AI] Job progressing");
					break;

				case Progress.Done:
					MyDebug.WriteLine("[AI] JOB DONE ({0})!", m_currentJob);
					World.TheWorld.Jobs.Remove(m_currentJob);
					m_currentJob = null;
					break;

				case Progress.Fail:
					MyDebug.WriteLine("[AI] JOB FAIL ({0})!!!", m_currentJob);
					World.TheWorld.Jobs.Remove(m_currentJob);
					m_currentJob = null;
					break;

				case Progress.Abort:
					MyDebug.WriteLine("[AI] JOB ABORT ({0})!!!", m_currentJob);
					m_currentJob = null;
					break;

				default:
					throw new Exception();
			}
		}

		public void ActionRequired()
		{
			return;

			if (m_living == GameData.Data.CurrentObject)
				return;

			if (m_currentJob == null)
			{
				var job = FindJob();

				if (job == null)
				{
					DoIdle();
					return;
				}

				m_currentJob = job;
			}

			var progress = m_currentJob.PrepareNextAction();

			switch (progress)
			{
				case Progress.Ok:
					var action = m_currentJob.CurrentAction;
					if (action == null)
						throw new Exception();

					m_living.EnqueueAction(action);
					break;

				case Progress.Done:
					MyDebug.WriteLine("[AI] JOB DONE ({0})!", m_currentJob);
					World.TheWorld.Jobs.Remove(m_currentJob);
					m_currentJob = null;
					DoIdle();
					break;

				case Progress.Fail:
					MyDebug.WriteLine("[AI] JOB FAIL ({0})!!!", m_currentJob);
					World.TheWorld.Jobs.Remove(m_currentJob);
					m_currentJob = null;
					DoIdle();
					break;

				case Progress.Abort:
					MyDebug.WriteLine("[AI] JOB ABORT ({0})!!!", m_currentJob);
					m_currentJob = null;
					DoIdle();
					break;

				case Progress.None:
				default:
					throw new Exception();
			}
		}

		void DoIdle()
		{
			MyDebug.WriteLine("[AI] no job to do");
			var action = new WaitAction(1);
			m_living.EnqueueAction(action);
		}

		IJob FindJob()
		{
			// XXX we modify jobs, so as quickfix make a copy
			IJob[] jobs = m_living.World.Jobs.Where(j => j.Worker == null).ToArray();

			foreach (var job in jobs)
			{
				var progress = job.Assign(m_living);

				switch (progress)
				{
					case Progress.Ok:
						MyDebug.WriteLine("[AI] new job {0}", job);
						return job;

					case Progress.Done:
						MyDebug.WriteLine("[AI] JOB (already) DONE!");
						World.TheWorld.Jobs.Remove(job);
						break;

					case Progress.Fail:
						MyDebug.WriteLine("[AI] JOB FAIL ({0})!!!", job);
						World.TheWorld.Jobs.Remove(job);
						break;

					case Progress.Abort:
						MyDebug.WriteLine("[AI] JOB ABORT ({0})!!!", job);
						break;

					case Progress.None:
					default:
						throw new Exception();
				}
			}

			return null;
		}
	}
}
