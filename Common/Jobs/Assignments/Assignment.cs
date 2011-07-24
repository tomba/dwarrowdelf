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
			this.JobStatus = Jobs.JobStatus.Ok;
		}

		protected Assignment(SaveGameContext ctx)
		{
		}

		[SaveGameProperty]
		public IJob Parent { get; private set; }
		[SaveGameProperty]
		public ActionPriority Priority { get; private set; }

		public bool IsAssigned
		{
			get
			{
				Debug.Assert(m_worker == null || this.JobStatus == Jobs.JobStatus.Ok);
				return m_worker != null;
			}
		}

		[SaveGameProperty]
		public JobStatus JobStatus { get; private set; }

		public void Abort()
		{
			SetState(JobStatus.Abort);
		}

		public IEnumerable<IAssignment> GetAssignments(ILiving living)
		{
			if (!this.IsAssigned)
				yield return this;
		}

		ILiving m_worker;
		[SaveGameProperty]
		public ILiving Worker
		{
			get { return m_worker; }
			private set { if (m_worker == value) return; m_worker = value; Notify("Worker"); }
		}

		GameAction m_action;
		[SaveGameProperty]
		public virtual GameAction CurrentAction
		{
			get { return m_action; }
			private set { if (m_action == value) return; m_action = value; Notify("CurrentAction"); }
		}

		public JobStatus Assign(ILiving worker)
		{
			Debug.Assert(this.IsAssigned == false);
			Debug.Assert(this.JobStatus == JobStatus.Ok);

			var state = AssignOverride(worker);
			SetState(state);
			if (state != JobStatus.Ok)
				return state;

			this.Worker = worker;

			return state;
		}

		protected virtual JobStatus AssignOverride(ILiving worker)
		{
			return JobStatus.Ok;
		}



		public JobStatus PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			JobStatus status;
			var action = PrepareNextActionOverride(out status);
			Debug.Assert((action == null && status != Jobs.JobStatus.Ok) || (action != null && status == Jobs.JobStatus.Ok));
			this.CurrentAction = action;
			SetState(status);
			return status;
		}

		protected abstract GameAction PrepareNextActionOverride(out JobStatus status);

		public JobStatus ActionProgress()
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.JobStatus == JobStatus.Ok);
			Debug.Assert(this.CurrentAction != null);

			var state = ActionProgressOverride();
			SetState(state);

			return state;
		}

		protected virtual JobStatus ActionProgressOverride()
		{
			return JobStatus.Ok;
		}

		public JobStatus ActionDone(ActionState actionStatus)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.JobStatus == JobStatus.Ok);
			Debug.Assert(this.CurrentAction != null);

			var state = ActionDoneOverride(actionStatus);
			SetState(state);

			this.CurrentAction = null;

			return state;
		}

		protected virtual JobStatus ActionDoneOverride(ActionState actionStatus)
		{
			switch (actionStatus)
			{
				case ActionState.Done:
					return JobStatus.Done;

				case ActionState.Fail:
					return JobStatus.Fail;

				case ActionState.Abort:
					return JobStatus.Abort;

				default:
					throw new Exception();
			}
		}

		void SetState(JobStatus status)
		{
			if (this.JobStatus == status)
				return;

			CheckStateChangeValidity(status);

			switch (status)
			{
				case JobStatus.Ok:
					break;

				case JobStatus.Done:
				case JobStatus.Abort:
				case JobStatus.Fail:
					this.Worker = null;
					this.CurrentAction = null;
					break;
			}

			this.JobStatus = status;
			OnStateChanged(status);
			if (this.StatusChanged != null)
				StatusChanged(this, status);
			Notify("JobStatus");
		}

		void CheckStateChangeValidity(JobStatus status)
		{
			switch (status)
			{
				case JobStatus.Ok:
					throw new Exception();

				case JobStatus.Done:
				case JobStatus.Abort:
				case JobStatus.Fail:
					if (this.JobStatus != JobStatus.Ok)
						throw new Exception();
					break;
			}
		}

		public event Action<IJob, JobStatus> StatusChanged;

		protected virtual void OnStateChanged(JobStatus status) { }

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
