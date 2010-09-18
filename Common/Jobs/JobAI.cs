using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Dwarrowdelf.Jobs;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs
{
	/// <summary>
	/// abstract AI that handles Jobs
	/// </summary>
	public abstract class JobAI : IAI
	{
		public ILiving Worker { get; private set; }
		IActionJob m_currentJob;

		protected JobAI(ILiving worker)
		{
			this.Worker = worker;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		protected void D(string format, params object[] args)
		{
			Debug.Print("[AI {0}]: {1}", this.Worker, String.Format(format, args));
		}

		protected virtual bool CheckForAbortOurJob(ActionPriority priority) { return false; }
		protected virtual bool CheckForAbortOtherAction(ActionPriority priority) { return false; }

		public GameAction DecideAction(ActionPriority priority)
		{
			D("DecideAction({0}): Worker.Action = {1}, CurrentJob {2}, CurrentJob.Action = {3}",
				priority,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				m_currentJob != null ? m_currentJob.ToString() : "<none>",
				m_currentJob != null && m_currentJob.CurrentAction != null ? m_currentJob.CurrentAction.ToString() : "<none>");

			if (m_currentJob != null)
			{
				// Action progress should keep us in sync
				if (m_currentJob.CurrentAction == null)
					Debug.Assert(this.Worker.CurrentAction == null);
				else
					Debug.Assert(m_currentJob.CurrentAction.MagicNumber == this.Worker.CurrentAction.MagicNumber);
			}

			if (this.Worker.HasAction && this.Worker.CurrentAction.Priority > priority)
			{
				D("DecideAction: worker already doing higher priority action");
				return null;
			}

			if (m_currentJob != null)
			{
				var abort = CheckForAbortOurJob(priority);
				if (abort)
				{
					D("DecideAction: will abort current job");
					m_currentJob.Abort();
					m_currentJob = null;
				}
				else if(this.Worker.HasAction)
				{
					D("DecideAction: doing our action, proceed doing it");
					return null;
				}
			}
			else if(this.Worker.HasAction)
			{
				var abort = CheckForAbortOtherAction(priority);
				if (abort)
				{
					D("DecideAction: will abort other action");
				}
				else
				{
					D("DecideAction: worker already doing other action");
					return null;
				}
			}

			while (true)
			{
				if (m_currentJob == null)
				{
					D("DecideAction: trying to find a new job");

					m_currentJob = FindAndAssignJob(this.Worker, priority);

					if (m_currentJob == null)
					{
						D("DecideAction: no job to do");
						return null;
					}
					else
					{
						D("DecideAction: new job: {0}", m_currentJob);
						m_currentJob.PropertyChanged += OnJobPropertyChanged;
					}
				}

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

						D("DecideAction: new {0}", action);
						return action;

					case Progress.Done:
					case Progress.Fail:
					case Progress.Abort:
						D("DecideAction: {0} in {1}, looking for new job", progress, m_currentJob);
						m_currentJob = null;
						break;

					case Progress.None:
						throw new Exception();
				}
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

		public void ActionStarted(ActionStartedChange change)
		{
			D("ActionStarted({0}, left {1}): Worker.Action = {2}, CurrentJob {3}, CurrentJob.Action = {4}",
				change.Action, change.TicksLeft,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				m_currentJob != null ? m_currentJob.ToString() : "<none>",
				m_currentJob != null && m_currentJob.CurrentAction != null ? m_currentJob.CurrentAction.ToString() : "<none>");

			if (m_currentJob == null)
			{
				D("ActionStarted: no job, so not for me");
				return;
			}

			if (m_currentJob.CurrentAction == null)
			{
				D("ActionStarted: action started by someone else, cancel our current job");
				m_currentJob.Abort();
				m_currentJob = null;
				return;
			}

			if (m_currentJob.CurrentAction.MagicNumber != change.Action.MagicNumber)
			{
				D("ActionStarted: action started by someone else, cancel our current job");
				throw new Exception();
			}

			// otherwise, it's an action started by us, all ok.
		}

		public void ActionProgress(ActionProgressChange e)
		{
			D("ActionProgress({0}, State {1}): Worker.Action = {2}, CurrentJob {3}, CurrentJob.Action = {4}",
				e.ActionXXX, e.State,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				m_currentJob != null ? m_currentJob.ToString() : "<none>",
				m_currentJob != null && m_currentJob.CurrentAction != null ? m_currentJob.CurrentAction.ToString() : "<none>");

			Debug.Assert(this.Worker.HasAction);
			Debug.Assert(e.ActionXXX.MagicNumber == this.Worker.CurrentAction.MagicNumber);

			if (m_currentJob == null)
			{
				D("ActionProgress: no job, so not for me");
				return;
			}

			if (e.State == ActionState.Abort && m_currentJob.CurrentAction != null &&
				m_currentJob.CurrentAction.MagicNumber != e.ActionXXX.MagicNumber)
			{
				D("ActionProgress: cancel event for action not started by us, ignore");
				return;
			}

			if (m_currentJob.CurrentAction == null)
			{
				throw new NotImplementedException("implement cancel work");
			}

			// does the action originate from us?
			if (m_currentJob.CurrentAction.MagicNumber != e.ActionXXX.MagicNumber)
			{
				throw new NotImplementedException("implement cancel work");
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
			}
		}
	}
}
