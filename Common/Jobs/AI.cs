using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using MyGame.Jobs;
using System.Diagnostics;

namespace MyGame.Jobs
{
	public interface IAI
	{
		GameAction ActionRequired(ActionPriority priority);
		void ActionProgress(ActionProgressChange e);
	}

	public abstract class AI : IAI
	{
		public ILiving Worker { get; private set; }
		IActionJob m_currentJob;

		protected AI(ILiving worker)
		{
			this.Worker = worker;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		protected void D(string format, params object[] args)
		{
			Debug.Print("[AI] [{0}]: {1}", this.Worker, String.Format(format, args));
		}

		public virtual GameAction ActionRequired(ActionPriority priority)
		{
			if (this.Worker.HasAction && this.Worker.CurrentAction.Priority >= priority)
				return null;

			D("ActionRequired({0}): CurrentJob {1}, Worker.Action = {2}", priority, m_currentJob, this.Worker.CurrentAction);

			while (true)
			{
				if (m_currentJob == null)
				{
					m_currentJob = FindAndAssignJob(this.Worker, priority);

					if (m_currentJob == null)
					{
						D("ActionRequired: no job to do");
						return null;
					}
					else
					{
						D("ActionRequired: new {0}", m_currentJob);
						m_currentJob.PropertyChanged += OnJobPropertyChanged;
					}
				}
				/*
				if (m_currentJob.CurrentAction != null)
				{
					D("ActionRequired: Already have an action");
					return null;
				}
				*/

				if (m_currentJob.Priority != priority)
					return null;

				Debug.Assert(m_currentJob.CurrentAction == null);

				var progress = m_currentJob.PrepareNextAction();

				switch (progress)
				{
					case Progress.Ok:
						var action = m_currentJob.CurrentAction;
						if (action == null)
							throw new Exception();

						D("ActionRequired: new {0}", action);
						return action;

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

		public virtual void ActionProgress(ActionProgressChange e)
		{
			D("ActionProgress: {0}, state: {1}", e.ActionXXX, e.State);

			if (m_currentJob == null)
			{
				D("ActionProgress: no job, so not for me");
				return;
			}

			if (m_currentJob.CurrentAction == null)
			{
				D("ActionProgress: no current action, so not for me");
				return;
			}

			// does the action originate from us?
			if (m_currentJob.CurrentAction.MagicNumber != e.ActionXXX.MagicNumber)
			{
				D("ActionProgress: wrong magic, so not for me");
				return;
			}

			Debug.Assert(e.ObjectID == this.Worker.ObjectID);

			var progress = m_currentJob.ActionProgress(e);

			switch (progress)
			{
				case Progress.None:
					throw new Exception();

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

		protected abstract IActionJob GetJob(ILiving worker, ActionPriority priority);

		IActionJob FindAndAssignJob(ILiving worker, ActionPriority priority)
		{
			while (true)
			{
				var job = GetJob(worker, priority);

				if (job == null)
					return null;

				var progress = job.Assign(worker);

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

		void OnJobPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Debug.Assert(sender == m_currentJob);

			IJob job = (IJob)sender;
			if (e.PropertyName == "Progress")
			{
				if (job.Progress == Progress.Abort)
				{
					job.PropertyChanged -= OnJobPropertyChanged;
					m_currentJob = null;
				}
			}
		}
	}

	public class ClientAI : AI
	{
		JobManager m_jobManager;

		public ClientAI(ILiving worker, JobManager jobManager)
			: base(worker)
		{
			m_jobManager = jobManager;
		}

		protected override IActionJob GetJob(ILiving worker, ActionPriority priority)
		{
			return m_jobManager.FindJob(this.Worker);
		}
	}
}
