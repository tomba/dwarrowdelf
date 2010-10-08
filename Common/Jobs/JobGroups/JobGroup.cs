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
		ObservableCollection<IJob> m_subJobs = new ObservableCollection<IJob>();
		ReadOnlyObservableCollection<IJob> m_roSubJobs;

		protected JobGroup(IJob parent, ActionPriority priority)
		{
			this.Parent = parent;
			this.Priority = priority;
			m_subJobs = new ObservableCollection<IJob>();
			m_roSubJobs = new ReadOnlyObservableCollection<IJob>(m_subJobs);
		}

		public JobType JobType { get { return JobType.JobGroup; } }
		public IJob Parent { get; private set; }
		public ActionPriority Priority { get; private set; }

		public virtual JobState JobState
		{
			get
			{
				if (this.SubJobs.All(j => j.JobState == JobState.Done))
					return JobState.Done;

				if (this.SubJobs.Any(j => j.JobState == JobState.Fail))
					return JobState.Fail;

				return JobState.Ok;
			}
		}

		public void Retry()
		{
			foreach (var job in m_subJobs.Where(j => j.JobState == Jobs.JobState.Abort))
				job.Retry();
		}

		public void Abort()
		{
			foreach (var job in m_subJobs.Where(j => j.JobState == Jobs.JobState.Ok))
				job.Abort();
		}

		public void Fail()
		{
			foreach (var job in m_subJobs)
				job.Fail();
		}

		public ReadOnlyObservableCollection<IJob> SubJobs { get { return m_roSubJobs; } }

		public abstract JobGroupType JobGroupType { get; }

		protected void AddSubJob(IJob job)
		{
			m_subJobs.Add(job);
			job.PropertyChanged += SubJobPropertyChanged;
		}

		void SubJobPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "JobState")
			{
				Notify("JobState");

				if (this.JobState == Jobs.JobState.Done || this.JobState == Jobs.JobState.Fail)
					Cleanup();
			}
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

		public override JobState JobState
		{
			get
			{
				var progress = base.JobState;

				return progress;
			}
		}

		public override JobGroupType JobGroupType { get { return JobGroupType.Parallel; } }
	}


	public abstract class SerialJobGroup : JobGroup
	{
		protected SerialJobGroup(IJobGroup parent, ActionPriority priority)
			: base(parent, priority)
		{
		}

		public override JobState JobState
		{
			get
			{
				var progress = base.JobState;

				if (progress != JobState.Ok)
					return progress;

				if (this.SubJobs.Any(j => j.JobState == JobState.Abort))
					return JobState.Abort;

				return JobState.Ok;
			}
		}

		public override JobGroupType JobGroupType { get { return JobGroupType.Serial; } }
	}
}
