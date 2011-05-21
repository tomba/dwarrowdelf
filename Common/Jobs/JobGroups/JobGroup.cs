using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public abstract class JobGroup : IJobGroup
	{
		ObservableCollection<IJob> m_subJobs;
		ReadOnlyObservableCollection<IJob> m_roSubJobs;

		protected JobGroup(IJob parent, ActionPriority priority)
		{
			this.Parent = parent;
			this.Priority = priority;
		}

		protected void SetSubJobs(IEnumerable<IJob> jobs)
		{
			if (m_subJobs != null)
				throw new Exception();

			m_subJobs = new ObservableCollection<IJob>(jobs);
			m_roSubJobs = new ReadOnlyObservableCollection<IJob>(m_subJobs);

			foreach (var job in jobs)
				job.StatusChanged += OnJobStatusChanged;
		}

		public JobType JobType { get { return JobType.JobGroup; } }
		public IJob Parent { get; private set; }
		public ActionPriority Priority { get; private set; }
		public JobStatus JobStatus { get; private set; }

		protected virtual JobStatus GetJobStatus()
		{
			if (this.SubJobs.All(j => j.JobStatus == JobStatus.Done))
				return JobStatus.Done;

			if (this.SubJobs.Any(j => j.JobStatus == JobStatus.Fail))
				return JobStatus.Fail;

			return JobStatus.Ok;
		}

		public event Action<IJob, JobStatus> StatusChanged;

		public void Retry()
		{
			foreach (var job in m_subJobs.Where(j => j.JobStatus == Jobs.JobStatus.Abort))
				job.Retry();
		}

		public void Abort()
		{
			foreach (var job in m_subJobs.Where(j => j.JobStatus == Jobs.JobStatus.Ok))
				job.Abort();
		}

		public void Fail()
		{
			foreach (var job in m_subJobs)
				job.Fail();
		}

		public ReadOnlyObservableCollection<IJob> SubJobs { get { return m_roSubJobs; } }

		public abstract JobGroupType JobGroupType { get; }

		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			this.JobStatus = GetJobStatus();

			if (this.StatusChanged != null)
				StatusChanged(this, this.JobStatus);

			Notify("JobStatus");

			if (this.JobStatus == Jobs.JobStatus.Done || this.JobStatus == Jobs.JobStatus.Fail)
				Cleanup();
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


	public abstract class ParallelJobGroup : JobGroup
	{
		protected ParallelJobGroup(IJob parent, ActionPriority priority)
			: base(parent, priority)
		{
		}

		protected override JobStatus GetJobStatus()
		{
			var progress = base.GetJobStatus();

			return progress;
		}

		public override JobGroupType JobGroupType { get { return JobGroupType.Parallel; } }
	}


	public abstract class SerialJobGroup : JobGroup
	{
		protected SerialJobGroup(IJobGroup parent, ActionPriority priority)
			: base(parent, priority)
		{
		}

		protected override JobStatus GetJobStatus()
		{
			var progress = base.GetJobStatus();

			if (progress != JobStatus.Ok)
				return progress;

			if (this.SubJobs.Any(j => j.JobStatus == JobStatus.Abort))
				return JobStatus.Abort;

			return JobStatus.Ok;
		}

		public override JobGroupType JobGroupType { get { return JobGroupType.Serial; } }
	}
}
