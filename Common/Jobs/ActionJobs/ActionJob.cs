using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.ActionJobs
{
	public abstract class ActionJob : IActionJob
	{
		protected ActionJob(IJob parent, ActionPriority priority)
		{
			this.Parent = parent;
			this.Priority = priority;
		}

		public IJob Parent { get; private set; }
		public ActionPriority Priority { get; private set; }

		ILiving m_worker;
		public ILiving Worker
		{
			get { return m_worker; }
			private set { m_worker = value; Notify("Worker"); }
		}

		GameAction m_action;
		public virtual GameAction CurrentAction
		{
			get { return m_action; }
			private set { m_action = value; Notify("CurrentAction"); }
		}

		Progress m_progress;
		public Progress Progress
		{
			get { return m_progress; }
			private set { m_progress = value; Notify("Progress"); }
		}


		public void Abort()
		{
			SetProgress(Progress.Abort);
		}

		protected virtual void AbortOverride()
		{
		}

		public Progress Assign(ILiving worker)
		{
			Debug.Assert(this.Worker == null);
			Debug.Assert(this.Progress == Progress.None || this.Progress == Progress.Abort);

			var progress = AssignOverride(worker);
			SetProgress(progress);
			if (progress != Progress.Ok)
				return progress;

			this.Worker = worker;

			return progress;
		}

		protected virtual Progress AssignOverride(ILiving worker)
		{
			return Progress.Ok;
		}



		public Progress PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			Progress progress;
			var action = PrepareNextActionOverride(out progress);
			Debug.Assert((action == null && progress != Jobs.Progress.Ok) || (action != null && progress == Jobs.Progress.Ok));
			this.CurrentAction = action;
			SetProgress(progress);
			return progress;
		}

		protected abstract GameAction PrepareNextActionOverride(out Progress progress);

		public Progress ActionProgress(ActionProgressChange e)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.Progress == Progress.Ok);
			Debug.Assert(this.CurrentAction != null);

			var progress = ActionProgressOverride(e);
			SetProgress(progress);

			if (e.TicksLeft == 0)
				this.CurrentAction = null;

			return progress;
		}

		protected virtual Progress ActionProgressOverride(ActionProgressChange e)
		{
			return Progress.Ok;
		}


		protected void SetProgress(Progress progress)
		{
			switch (progress)
			{
				case Progress.None:
					break;

				case Progress.Ok:
					break;

				case Progress.Done:
					Cleanup();
					this.Worker = null;
					this.CurrentAction = null;
					break;

				case Progress.Abort:
					this.Worker = null;
					this.CurrentAction = null;
					break;

				case Progress.Fail:
					Cleanup();
					this.Worker = null;
					this.CurrentAction = null;
					break;
			}

			this.Progress = progress;
		}

		protected virtual void Cleanup()
		{
		}


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
