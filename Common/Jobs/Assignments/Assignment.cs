using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	public abstract class Assignment : IAssignment
	{
		protected Assignment(IJob parent, ActionPriority priority)
		{
			this.Parent = parent;
			this.Priority = priority;
		}

		public JobType JobType { get { return JobType.Assignment; } }
		public IJob Parent { get; private set; }
		public ActionPriority Priority { get; private set; }

		public bool IsAssigned
		{
			get
			{
				Debug.Assert(m_worker == null || this.JobState == Jobs.JobState.Ok);
				return m_worker != null;
			}
		}

		public JobState JobState { get; private set; }

		public void Retry()
		{
			Debug.Assert(this.JobState != JobState.Ok);
			Debug.Assert(this.IsAssigned == false);

			SetState(JobState.Ok);
		}

		public void Abort()
		{
			SetState(JobState.Abort);
		}

		public void Fail()
		{
			SetState(JobState.Fail);
		}

		ILiving m_worker;
		public ILiving Worker
		{
			get { return m_worker; }
			private set { if (m_worker == value) return; m_worker = value; Notify("Worker"); }
		}

		GameAction m_action;
		public virtual GameAction CurrentAction
		{
			get { return m_action; }
			private set { if (m_action == value) return; m_action = value; Notify("CurrentAction"); }
		}

		public JobState Assign(ILiving worker)
		{
			Debug.Assert(this.IsAssigned == false);
			Debug.Assert(this.JobState == JobState.Ok);

			var state = AssignOverride(worker);
			SetState(state);
			if (state != JobState.Ok)
				return state;

			this.Worker = worker;

			return state;
		}

		protected virtual JobState AssignOverride(ILiving worker)
		{
			return JobState.Ok;
		}



		public JobState PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			JobState state;
			var action = PrepareNextActionOverride(out state);
			Debug.Assert((action == null && state != Jobs.JobState.Ok) || (action != null && state == Jobs.JobState.Ok));
			this.CurrentAction = action;
			SetState(state);
			return state;
		}

		protected abstract GameAction PrepareNextActionOverride(out JobState state);

		public JobState ActionProgress(ActionProgressChange e)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.JobState == JobState.Ok);
			Debug.Assert(this.CurrentAction != null);

			var state = ActionProgressOverride(e);
			SetState(state);

			if (e.TicksLeft == 0)
				this.CurrentAction = null;

			return state;
		}

		protected virtual JobState ActionProgressOverride(ActionProgressChange e)
		{
			return JobState.Ok;
		}

		void SetState(JobState state)
		{
			if (this.JobState == state)
				return;

			switch (state)
			{
				case JobState.Ok:
					break;

				case JobState.Done:
					Debug.Assert(this.JobState == JobState.Ok);
					break;

				case JobState.Abort:
					Debug.Assert(this.JobState == JobState.Ok || this.JobState == JobState.Done);
					break;

				case JobState.Fail:
					Debug.Assert(this.JobState == JobState.Ok);
					break;
			}

			switch (state)
			{
				case JobState.Ok:
					break;

				case JobState.Done:
				case JobState.Abort:
				case JobState.Fail:
					this.Worker = null;
					this.CurrentAction = null;
					break;
			}

			this.JobState = state;
			OnStateChanged(state);
			if (this.StateChanged != null)
				StateChanged(this, state);
			Notify("JobState");
		}

		public event Action<IJob, JobState> StateChanged;

		protected virtual void OnStateChanged(JobState state) { }

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
