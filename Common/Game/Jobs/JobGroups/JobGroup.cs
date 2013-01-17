using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.JobGroups
{
	[SaveGameObject]
	public abstract class JobGroup : IJobGroup, IJobObserver
	{
		[SaveGameProperty]
		ObservableCollection<IJob> m_subJobs;
		ReadOnlyObservableCollection<IJob> m_roSubJobs;

		protected JobGroup(IJobObserver parent)
		{
			this.Parent = parent;

			m_subJobs = new ObservableCollection<IJob>();
			m_roSubJobs = new ReadOnlyObservableCollection<IJob>(m_subJobs);

			this.Status = JobStatus.Ok;
		}

		protected JobGroup(SaveGameContext ctx)
		{
			m_roSubJobs = new ReadOnlyObservableCollection<IJob>(m_subJobs);
		}

		protected void AddSubJob(IJob job)
		{
			m_subJobs.Add(job);

			if (job.Status != JobStatus.Ok)
			{
				throw new Exception();
				// XXX I think job should be always Ok when it is constructed. Init() method for jobs?
				//OnObserverJobStatusChanged(job, job.Status);
				//return;
			}
		}

		protected void AddSubJobs(IEnumerable<IJob> jobs)
		{
			foreach (var job in jobs)
				AddSubJob(job);
		}

		protected void RemoveSubJob(IJob job)
		{
			Debug.Assert(m_subJobs.Contains(job));

			m_subJobs.Remove(job);
		}

		[SaveGameProperty]
		public IJobObserver Parent { get; private set; }
		[SaveGameProperty]
		public JobStatus Status { get; private set; }

		protected virtual void OnStatusChanged(JobStatus status) { }

		public event Action<IJob, JobStatus> StatusChanged;

		public void Abort()
		{
			SetStatus(JobStatus.Abort);
		}

		public ReadOnlyObservableCollection<IJob> SubJobs { get { return m_roSubJobs; } }

		public IAssignment FindAssignment(ILivingObject living)
		{
			foreach (var job in GetJobs(living))
			{
				var assignment = job as IAssignment;
				if (assignment != null)
				{
					if (assignment.IsAssigned == false)
						return assignment;
				}
				else
				{
					var jobGroup = (IJobGroup)job;
					assignment = jobGroup.FindAssignment(living);
					if (assignment != null)
						return assignment;
				}
			}

			return null;
		}

		protected virtual IEnumerable<IJob> GetJobs(ILivingObject living)
		{
			return this.SubJobs.Where(j => j.Status == JobStatus.Ok);
		}

		public IEnumerable<IAssignment> GetAssignments(ILivingObject living)
		{
			foreach (var job in GetJobs(living))
			{
				var assignment = job as IAssignment;
				if (assignment != null)
				{
					if (assignment.IsAssigned == false)
						yield return assignment;
				}
				else
				{
					var jobGroup = (IJobGroup)job;

					foreach (var a in jobGroup.GetAssignments(living))
						yield return a;
				}
			}
		}

		void IJobObserver.OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			switch (status)
			{
				case JobStatus.Ok:
					throw new Exception();

				case JobStatus.Abort:
					OnSubJobAborted(job);
					break;

				case JobStatus.Fail:
					OnSubJobFailed(job);
					break;

				case JobStatus.Done:
					OnSubJobDone(job);
					break;
			}
		}

		protected virtual void OnSubJobAborted(IJob job)
		{
			SetStatus(JobStatus.Abort);
		}

		protected virtual void OnSubJobFailed(IJob job)
		{
			SetStatus(JobStatus.Fail);
		}

		protected virtual void OnSubJobDone(IJob job)
		{
			SetStatus(JobStatus.Done);
		}

		protected void SetStatus(JobStatus status)
		{
			if (this.Status == status)
				return;

			if (status == JobStatus.Ok)
				throw new Exception();

			if (this.Status != JobStatus.Ok)
				throw new Exception();

			this.Status = status;

			Cleanup();

			foreach (var job in m_subJobs.ToArray())
			{
				if (job.Status == JobStatus.Ok)
					job.Abort();
			}

			m_subJobs.Clear();
			m_subJobs = null;
			m_roSubJobs = null;

			OnStatusChanged(status);

			if (this.Parent != null)
				this.Parent.OnObservableJobStatusChanged(this, status);

			if (this.StatusChanged != null)
				StatusChanged(this, status);

			Notify("JobStatus");
		}

		protected virtual void Cleanup()
		{
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
