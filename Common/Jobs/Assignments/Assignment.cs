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
		protected Assignment(IJobObserver parent)
		{
			this.Parent = parent;
			this.Status = JobStatus.Ok;
		}

		protected Assignment(SaveGameContext ctx)
		{
		}

		[SaveGameProperty]
		public IJobObserver Parent { get; private set; }

		public bool IsAssigned
		{
			get
			{
				Debug.Assert(m_worker == null || this.Status == JobStatus.Ok);
				return m_worker != null;
			}
		}

		[SaveGameProperty]
		public JobStatus Status { get; private set; }

		public void Abort()
		{
			SetState(JobStatus.Abort);
		}

		ILivingObject m_worker;
		[SaveGameProperty]
		public ILivingObject Worker
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

		[SaveGameProperty]
		public LaborID LaborID { get; protected set; }

		public void Assign(ILivingObject worker)
		{
			Debug.Assert(this.IsAssigned == false);
			Debug.Assert(this.Status == JobStatus.Ok);

			this.Worker = worker;

			AssignOverride(worker);

			Debug.Assert(this.Status == JobStatus.Ok);
		}

		protected virtual void AssignOverride(ILivingObject worker)
		{
		}



		public JobStatus PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			JobStatus status;
			var action = PrepareNextActionOverride(out status);
			Debug.Assert((action == null && status != JobStatus.Ok) || (action != null && status == JobStatus.Ok));
			this.CurrentAction = action;
			SetState(status);
			return status;
		}

		protected abstract GameAction PrepareNextActionOverride(out JobStatus status);

		public JobStatus ActionProgress()
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.Status == JobStatus.Ok);
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
			Debug.Assert(this.Status == JobStatus.Ok);
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
			if (this.Status == status)
				return;

			if (status == JobStatus.Ok)
				throw new Exception();

			this.Worker = null;
			this.CurrentAction = null;

			this.Status = status;

			OnStateChanged(status);

			if (this.Parent != null)
				this.Parent.OnObservableJobStatusChanged(this, status);

			if (this.StatusChanged != null)
				StatusChanged(this, status);
			Notify("JobStatus");
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
