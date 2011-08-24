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

	public interface IJob : INotifyPropertyChanged
	{
		IJob Parent { get; }
		JobStatus JobStatus { get; }
		void Abort();

		IEnumerable<IAssignment> GetAssignments(ILiving living);

		event Action<IJob, JobStatus> StatusChanged;
	}

	public interface IJobGroup : IJob
	{
	}

	public interface IAssignment : IJob
	{
		bool IsAssigned { get; }
		ILiving Worker { get; }
		GameAction CurrentAction { get; }

		void Assign(ILiving worker);
		JobStatus PrepareNextAction();
		JobStatus ActionProgress();
		JobStatus ActionDone(ActionState actionStatus);
	}
}
