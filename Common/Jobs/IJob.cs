using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs
{
	public enum JobState
	{
		/// <summary>
		/// Everything ok
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

	public enum JobGroupType
	{
		Parallel,
		Serial,
	}

	public enum JobType
	{
		Assignment,
		JobGroup,
	}

	public interface IJob : INotifyPropertyChanged
	{
		JobType JobType { get; }
		IJob Parent { get; }
		ActionPriority Priority { get; }
		JobState JobState { get; }
		void Retry();
		void Abort();
		void Fail();
	}

	public interface IJobGroup : IJob
	{
		ReadOnlyObservableCollection<IJob> SubJobs { get; }
		JobGroupType JobGroupType { get; }
	}

	public interface IAssignment : IJob
	{
		bool IsAssigned { get; }
		ILiving Worker { get; }
		GameAction CurrentAction { get; }

		JobState Assign(ILiving worker);
		JobState PrepareNextAction();
		JobState ActionProgress(ActionProgressChange e);
	}
}
