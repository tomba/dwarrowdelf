using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Dwarrowdelf.Jobs;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs
{
	/// <summary>
	/// abstract AI that handles Assignments
	/// </summary>
	public abstract class AssignmentAI : IAI
	{
		public ILiving Worker { get; private set; }

		IAssignment m_currentAssignment;
		public IAssignment CurrentAssignment
		{
			get { return m_currentAssignment; }

			private set
			{
				if (m_currentAssignment != null)
					this.CurrentAssignment.PropertyChanged -= OnAssignmentPropertyChanged;

				m_currentAssignment = value;

				if (m_currentAssignment != null)
					this.CurrentAssignment.PropertyChanged += OnAssignmentPropertyChanged;

				if (AssignmentChanged != null)
					AssignmentChanged(value);
			}
		}

		public event Action<IAssignment> AssignmentChanged;

		protected AssignmentAI(ILiving worker)
		{
			this.Worker = worker;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		protected void D(string format, params object[] args)
		{
			//Debug.Print("[AI {0}]: {1}", this.Worker, String.Format(format, args));
		}

		protected virtual bool CheckForAbortOurAssignment(ActionPriority priority) { return false; }
		protected virtual bool CheckForAbortOtherAction(ActionPriority priority) { return false; }

		public GameAction DecideAction(ActionPriority priority)
		{
			D("DecideAction({0}): Worker.Action = {1}, CurrentAssignment {2}, CurrentAssignment.Action = {3}",
				priority,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				this.CurrentAssignment != null ? this.CurrentAssignment.ToString() : "<none>",
				this.CurrentAssignment != null && this.CurrentAssignment.CurrentAction != null ? this.CurrentAssignment.CurrentAction.ToString() : "<none>");

			if (this.CurrentAssignment != null)
			{
				// Action progress should keep us in sync
				if (this.CurrentAssignment.CurrentAction == null)
					Debug.Assert(this.Worker.CurrentAction == null);
				else
					Debug.Assert(this.CurrentAssignment.CurrentAction.MagicNumber == this.Worker.CurrentAction.MagicNumber);
			}

			if (this.Worker.HasAction && this.Worker.CurrentAction.Priority > priority)
			{
				D("DecideAction: worker already doing higher priority action");
				return null;
			}

			if (this.CurrentAssignment != null)
			{
				var abort = CheckForAbortOurAssignment(priority);
				if (abort)
				{
					D("DecideAction: will abort current assignment");
					this.CurrentAssignment.Abort();
					this.CurrentAssignment = null;
				}
				else if (this.Worker.HasAction)
				{
					D("DecideAction: doing our action, proceed doing it");
					return null;
				}
			}
			else if (this.Worker.HasAction)
			{
				var abort = CheckForAbortOtherAction(priority);
				if (abort)
				{
					D("DecideAction: will abort other action");
				}
				else
				{
					D("DecideAction: worker already doing other action");
					return null;
				}
			}

			while (true)
			{
				var assignment = this.CurrentAssignment;

				if (assignment == null)
				{
					D("DecideAction: trying to find a new assignment");

					assignment = FindAndAssignJob(this.Worker, priority);

					if (assignment == null)
					{
						D("DecideAction: no assignment to do");
						return null;
					}
					else
					{
						D("DecideAction: new assignment: {0}", assignment);
					}
				}

				this.CurrentAssignment = assignment;

				if (assignment.Priority != priority)
					return null;

				Debug.Assert(assignment.CurrentAction == null);

				var progress = assignment.PrepareNextAction();

				switch (progress)
				{
					case Progress.Ok:
						var action = assignment.CurrentAction;
						if (action == null)
							throw new Exception();

						D("DecideAction: new {0}", action);
						return action;

					case Progress.Done:
					case Progress.Fail:
					case Progress.Abort:
						D("DecideAction: {0} in {1}, looking for new assignment", progress, assignment);
						this.CurrentAssignment = assignment = null;
						break;

					case Progress.None:
						throw new Exception();
				}
			}
		}


		protected abstract IAssignment GetAssignment(ILiving worker, ActionPriority priority);

		IAssignment FindAndAssignJob(ILiving worker, ActionPriority priority)
		{
			int tries = 0;

			while (true)
			{
				if (tries++ > 10)
				{
					Trace.TraceWarning("Cannot find job for {0} after {1} tries", worker, tries);
					return null;
				}

				var assignment = GetAssignment(worker, priority);

				if (assignment == null)
					return null;

				var progress = assignment.Assign(worker);

				switch (progress)
				{
					case Progress.Ok:
						return assignment;

					case Progress.Done:
						break;

					case Progress.Fail:
						break;

					case Progress.Abort:
						break;

					case Progress.None:
						throw new Exception();
				}
			}
		}

		void OnAssignmentPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Debug.Assert(sender == this.CurrentAssignment);

			var assignment = (IAssignment)sender;
			if (e.PropertyName == "Progress")
			{
				if (assignment.Progress == Progress.Abort || assignment.Progress == Progress.Fail)
				{
					this.CurrentAssignment = null;
				}
			}
		}

		public void ActionStarted(ActionStartedChange change)
		{
			D("ActionStarted({0}, left {1}): Worker.Action = {2}, CurrentAssignment {3}, CurrentAssignment.Action = {4}",
				change.Action, change.TicksLeft,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				this.CurrentAssignment != null ? this.CurrentAssignment.ToString() : "<none>",
				this.CurrentAssignment != null && this.CurrentAssignment.CurrentAction != null ? this.CurrentAssignment.CurrentAction.ToString() : "<none>");

			if (this.CurrentAssignment == null)
			{
				D("ActionStarted: no assignment, so not for me");
				return;
			}

			if (this.CurrentAssignment.CurrentAction == null)
			{
				D("ActionStarted: action started by someone else, cancel our current assignment");
				this.CurrentAssignment.Abort();
				this.CurrentAssignment = null;
				return;
			}

			if (this.CurrentAssignment.CurrentAction.MagicNumber != change.Action.MagicNumber)
			{
				D("ActionStarted: action started by someone else, cancel our current assignment");
				throw new Exception();
			}

			// otherwise, it's an action started by us, all ok.
		}

		public void ActionProgress(ActionProgressChange e)
		{
			D("ActionProgress({0}, State {1}): Worker.Action = {2}, CurrentAssignment {3}, CurrentAssignment.Action = {4}",
				e.ActionXXX, e.State,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				this.CurrentAssignment != null ? this.CurrentAssignment.ToString() : "<none>",
				this.CurrentAssignment != null && this.CurrentAssignment.CurrentAction != null ? this.CurrentAssignment.CurrentAction.ToString() : "<none>");

			Debug.Assert(this.Worker.HasAction);
			Debug.Assert(e.ActionXXX.MagicNumber == this.Worker.CurrentAction.MagicNumber);

			if (this.CurrentAssignment == null)
			{
				D("ActionProgress: no assignment, so not for me");
				return;
			}

			if (e.State == ActionState.Abort && this.CurrentAssignment.CurrentAction != null &&
				this.CurrentAssignment.CurrentAction.MagicNumber != e.ActionXXX.MagicNumber)
			{
				D("ActionProgress: cancel event for action not started by us, ignore");
				return;
			}

			if (this.CurrentAssignment.CurrentAction == null)
			{
				throw new NotImplementedException("implement cancel work");
			}

			// does the action originate from us?
			if (this.CurrentAssignment.CurrentAction.MagicNumber != e.ActionXXX.MagicNumber)
			{
				throw new NotImplementedException("implement cancel work");
			}

			Debug.Assert(e.ObjectID == this.Worker.ObjectID);

			var progress = this.CurrentAssignment.ActionProgress(e);

			switch (progress)
			{
				case Progress.None:
					throw new Exception();

				case Progress.Ok:
					D("Assignment progressing");
					break;

				case Progress.Done:
				case Progress.Fail:
				case Progress.Abort:
					D("ActionProgress: {0} in {1}", progress, this.CurrentAssignment);
					this.CurrentAssignment = null;
					break;
			}
		}
	}
}
