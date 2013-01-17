using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs
{
	public enum JobStatus
	{
		/// <summary>
		/// Work ongoing
		/// </summary>
		Ok,
		/// <summary>
		/// Job failed, the worker wasn't able to do it
		/// </summary>
		Abort,
		/// <summary>
		/// Job failed, and nobody else can do it either
		/// </summary>
		Fail,
		/// <summary>
		/// Job has been done successfully
		/// </summary>
		Done,
	}

	public interface IJobObserver
	{
		void OnObservableJobStatusChanged(IJob job, JobStatus status);
	}

	public interface IJob : INotifyPropertyChanged
	{
		IJobObserver Parent { get; }
		JobStatus Status { get; }
		void Abort();

		event Action<IJob, JobStatus> StatusChanged;
	}

	public interface IJobGroup : IJob
	{
		IAssignment FindAssignment(ILivingObject living);
		IEnumerable<IAssignment> GetAssignments(ILivingObject living);
	}

	public interface IAssignment : IJob
	{
		bool IsAssigned { get; }
		ILivingObject Worker { get; }
		GameAction CurrentAction { get; }
		LaborID LaborID { get; }

		void Assign(ILivingObject worker);
		JobStatus PrepareNextAction();
		JobStatus ActionProgress();
		JobStatus ActionDone(ActionState actionStatus);
	}
}
