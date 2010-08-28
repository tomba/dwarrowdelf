using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using MyGame.Jobs;

namespace MyGame.Jobs
{
	public interface IAI
	{
		void ActionRequired();
		void ActionProgress(ActionProgressEvent e);
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
			MyDebug.WriteLine("[AI] [{0}]: {1}", this.Worker, String.Format(format, args));
		}

		public void ActionRequired()
		{
			while (true)
			{
				if (m_currentJob == null)
				{
					m_currentJob = FindAndAssignJob(this.Worker);

					if (m_currentJob == null)
					{
						D("no job to do");
						this.Worker.DoSkipAction();
						return;
					}
					else
					{
						m_currentJob.PropertyChanged += OnJobPropertyChanged;
					}
				}

				var progress = m_currentJob.PrepareNextAction();

				switch (progress)
				{
					case Progress.Ok:
						var action = m_currentJob.CurrentAction;
						if (action == null)
							throw new Exception();

						this.Worker.DoAction(action);
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

		protected abstract IActionJob GetJob(ILiving worker);

		IActionJob FindAndAssignJob(ILiving worker)
		{
			while (true)
			{
				var job = GetJob(worker);

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

		protected override IActionJob GetJob(ILiving worker)
		{
			return m_jobManager.FindJob(this.Worker);
		}
	}
}
