using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs
{
	public enum Progress
	{
		/// <summary>
		/// None
		/// </summary>
		None,
		/// <summary>
		/// Everything ok
		/// </summary>
		Ok,
		/// <summary>
		/// Job failed, and nobody else can do it either
		/// </summary>
		Fail,
		/// <summary>
		/// Job failed, the worker wasn't able to do it
		/// </summary>
		Abort,
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

	public interface IJob : INotifyPropertyChanged
	{
		IJob Parent { get; }
		ActionPriority Priority { get; }
		Progress Progress { get; }
		void Retry();
		void Abort();
	}

	public interface IJobGroup : IJob
	{
		ReadOnlyObservableCollection<IJob> SubJobs { get; }
		JobGroupType JobGroupType { get; }
	}

	public interface IAssignment : IJob
	{
		ILiving Worker { get; }
		GameAction CurrentAction { get; }

		Progress Assign(ILiving worker);
		Progress PrepareNextAction();
		Progress ActionProgress(ActionProgressChange e);
	}
}
