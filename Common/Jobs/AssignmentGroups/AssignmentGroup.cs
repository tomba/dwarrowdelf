using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public abstract class AssignmentGroup : IAssignment
	{
		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			//Debug.Print("[AI O] [{0}]: {1}", this.Worker, String.Format(format, args));
		}

		protected AssignmentGroup(IJob parent, ActionPriority priority)
		{
			this.Parent = parent;
			this.Priority = priority;
		}

		protected AssignmentGroup(SaveGameContext ctx)
		{
		}

		[SaveGameProperty]
		public IJob Parent { get; private set; }
		[SaveGameProperty]
		public ActionPriority Priority { get; private set; }
		[SaveGameProperty]
		public JobStatus JobStatus { get; private set; }

		public void Abort()
		{
			SetStatus(JobStatus.Abort);
		}

		public IEnumerable<IAssignment> GetAssignments(ILiving living)
		{
			if (!this.IsAssigned)
				yield return this;
		}

		public bool IsAssigned
		{
			get
			{
				Debug.Assert(m_worker == null || this.JobStatus == Jobs.JobStatus.Ok);
				return m_worker != null;
			}
		}

		ILiving m_worker;
		[SaveGameProperty]
		public ILiving Worker
		{
			get { return m_worker; }
			private set { if (m_worker == value) return; m_worker = value; Notify("Worker"); }
		}

		IAssignment m_assignment;
		[SaveGameProperty]
		public IAssignment CurrentAssignment
		{
			get { return m_assignment; }

			private set
			{
				if (m_assignment == value)
					return;

				if (m_assignment != null)
				{
					m_assignment.StatusChanged -= OnCurrentAssignmentStatusChanged;

					if (m_assignment.JobStatus == Jobs.JobStatus.Ok)
						m_assignment.Abort();
				}

				m_assignment = value;
				Notify("CurrentAssignment");

				if (m_assignment != null)
				{
					m_assignment.StatusChanged += OnCurrentAssignmentStatusChanged;
				}
			}
		}

		public GameAction CurrentAction
		{
			get { return this.CurrentAssignment != null ? this.CurrentAssignment.CurrentAction : null; }
		}

		public JobStatus Assign(ILiving worker)
		{
			Debug.Assert(this.IsAssigned == false);
			Debug.Assert(this.JobStatus == JobStatus.Ok);

			D("Assign {0}", worker);

			this.Worker = worker;

			var status = AssignOverride(worker);

			SetStatus(status);

			return this.JobStatus;
		}

		protected abstract JobStatus AssignOverride(ILiving worker);

		public JobStatus PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			D("PrepareNextAction");

			while (true)
			{
				while (this.CurrentAssignment == null || this.CurrentAssignment.JobStatus != Jobs.JobStatus.Ok)
				{
					var assignment = PrepareNextAssignment();

					if (this.JobStatus != Jobs.JobStatus.Ok)
						return this.JobStatus;

					Debug.Assert(assignment.JobStatus == Jobs.JobStatus.Ok);

					this.CurrentAssignment = assignment;

					var status = this.CurrentAssignment.Assign(this.Worker);

					if (this.JobStatus != Jobs.JobStatus.Ok)
						return this.JobStatus;

					if (status != Jobs.JobStatus.Ok)
						continue;
				}

				Debug.Assert(this.CurrentAssignment != null);
				Debug.Assert(this.CurrentAssignment.JobStatus == Jobs.JobStatus.Ok);

				{
					var status = this.CurrentAssignment.PrepareNextAction();
					Notify("CurrentAction");

					if (this.JobStatus != Jobs.JobStatus.Ok)
						return this.JobStatus;

					switch (status)
					{
						case JobStatus.Ok:
							Debug.Assert(this.CurrentAction != null);
							return JobStatus.Ok;

						case JobStatus.Done:
							continue;

						case JobStatus.Abort:
						case JobStatus.Fail:
							Debug.Assert(this.CurrentAction == null);
							continue;
					}
				}
			}
		}

		protected abstract IAssignment PrepareNextAssignment();

		public JobStatus ActionProgress()
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.JobStatus == JobStatus.Ok);
			Debug.Assert(this.CurrentAction != null);
			Debug.Assert(this.CurrentAssignment != null);

			D("ActionProgress");

			this.CurrentAssignment.ActionProgress();
			Notify("CurrentAction");

			return this.JobStatus;
		}

		public JobStatus ActionDone(ActionState actionStatus)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.JobStatus == JobStatus.Ok);
			Debug.Assert(this.CurrentAction != null);
			Debug.Assert(this.CurrentAssignment != null);

			D("ActionProgress");

			this.CurrentAssignment.ActionDone(actionStatus);
			Notify("CurrentAction");

			return this.JobStatus;
		}

		protected void SetStatus(JobStatus status)
		{
			if (this.JobStatus == status)
				return;

			D("SetState({0})", status);

			CheckStateChangeValidity(status);

			this.JobStatus = status;

			switch (status)
			{
				case JobStatus.Ok:
					break;

				case JobStatus.Done:
				case JobStatus.Abort:
				case JobStatus.Fail:
					this.Worker = null;
					this.CurrentAssignment = null;
					break;
			}

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

		void OnCurrentAssignmentStatusChanged(IJob job, JobStatus status)
		{
			Debug.Assert(job == this.CurrentAssignment);

			switch (status)
			{
				case Jobs.JobStatus.Ok:
					throw new Exception();

				case Jobs.JobStatus.Abort:
					OnAssignmentAborted();
					break;

				case Jobs.JobStatus.Fail:
					OnAssignmentFailed();
					break;

				case Jobs.JobStatus.Done:
					OnAssignmentDone();
					break;
			}
		}

		protected virtual void OnAssignmentAborted()
		{
			SetStatus(Jobs.JobStatus.Abort);
		}

		protected virtual void OnAssignmentFailed()
		{
			SetStatus(Jobs.JobStatus.Fail);
		}

		protected virtual void OnAssignmentDone()
		{
			SetStatus(Jobs.JobStatus.Done);
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		protected void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
