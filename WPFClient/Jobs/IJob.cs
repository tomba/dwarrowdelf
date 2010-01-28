using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MyGame.Client
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

	enum JobGroupType
	{
		Parallel,
		Serial,
	}

	interface IJob : INotifyPropertyChanged
	{
		IJob Parent { get; }
		Progress Progress { get; }
		void Abort();
	}

	interface IJobGroup : IJob
	{
		IList<IJob> SubJobs { get; }
		JobGroupType JobGroupType { get; }
	}

	interface IActionJob : IJob
	{
		Living Worker { get; }
		GameAction CurrentAction { get; }

		Progress Assign(Living worker);
		Progress PrepareNextAction();
		Progress ActionProgress(ActionProgressEvent e);
	}
}
