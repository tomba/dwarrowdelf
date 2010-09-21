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
		public IAssignment CurrentAssignment { get { return m_currentAssignment; } }

		protected AssignmentAI(ILiving worker)
		{
			this.Worker = worker;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		protected void D(string format, params object[] args)
		{
			Debug.Print("[AI {0}]: {1}", this.Worker, String.Format(format, args));
		}

		protected virtual bool CheckForAbortOurAssignment(ActionPriority priority) { return false; }
		protected virtual bool CheckForAbortOtherAction(ActionPriority priority) { return false; }

		public GameAction DecideAction(ActionPriority priority)
		{
			D("DecideAction({0}): Worker.Action = {1}, CurrentAssignment {2}, CurrentAssignment.Action = {3}",
				priority,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				m_currentAssignment != null ? m_currentAssignment.ToString() : "<none>",
				m_currentAssignment != null && m_currentAssignment.CurrentAction != null ? m_currentAssignment.CurrentAction.ToString() : "<none>");

			if (m_currentAssignment != null)
			{
				// Action progress should keep us in sync
				if (m_currentAssignment.CurrentAction == null)
					Debug.Assert(this.Worker.CurrentAction == null);
				else
					Debug.Assert(m_currentAssignment.CurrentAction.MagicNumber == this.Worker.CurrentAction.MagicNumber);
			}

			if (this.Worker.HasAction && this.Worker.CurrentAction.Priority > priority)
			{
				D("DecideAction: worker already doing higher priority action");
				return null;
			}

			if (m_currentAssignment != null)
			{
				var abort = CheckForAbortOurAssignment(priority);
				if (abort)
				{
					D("DecideAction: will abort current assignment");
					m_currentAssignment.Abort();
					m_currentAssignment = null;
				}
				else if(this.Worker.HasAction)
				{
					D("DecideAction: doing our action, proceed doing it");
					return null;
				}
			}
			else if(this.Worker.HasAction)
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
				if (m_currentAssignment == null)
				{
					D("DecideAction: trying to find a new assignment");

					m_currentAssignment = FindAndAssignJob(this.Worker, priority);

					if (m_currentAssignment == null)
					{
						D("DecideAction: no assignment to do");
						return null;
					}
					else
					{
						D("DecideAction: new assignment: {0}", m_currentAssignment);
						m_currentAssignment.PropertyChanged += OnAssignmentPropertyChanged;
					}
				}

				if (m_currentAssignment.Priority != priority)
					return null;

				Debug.Assert(m_currentAssignment.CurrentAction == null);

				var progress = m_currentAssignment.PrepareNextAction();

				switch (progress)
				{
					case Progress.Ok:
						var action = m_currentAssignment.CurrentAction;
						if (action == null)
							throw new Exception();

						D("DecideAction: new {0}", action);
						return action;

					case Progress.Done:
					case Progress.Fail:
					case Progress.Abort:
						D("DecideAction: {0} in {1}, looking for new assignment", progress, m_currentAssignment);
						m_currentAssignment = null;
						break;

					case Progress.None:
						throw new Exception();
				}
			}
		}


		protected abstract IAssignment GetAssignment(ILiving worker, ActionPriority priority);

		IAssignment FindAndAssignJob(ILiving worker, ActionPriority priority)
		{
			while (true)
			{
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
			Debug.Assert(sender == m_currentAssignment);

			var assignment = (IAssignment)sender;
			if (e.PropertyName == "Progress")
			{
				if (assignment.Progress == Progress.Abort || assignment.Progress == Progress.Fail)
				{
					assignment.PropertyChanged -= OnAssignmentPropertyChanged;
					m_currentAssignment = null;
				}
			}
		}

		public void ActionStarted(ActionStartedChange change)
		{
			D("ActionStarted({0}, left {1}): Worker.Action = {2}, CurrentAssignment {3}, CurrentAssignment.Action = {4}",
				change.Action, change.TicksLeft,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				m_currentAssignment != null ? m_currentAssignment.ToString() : "<none>",
				m_currentAssignment != null && m_currentAssignment.CurrentAction != null ? m_currentAssignment.CurrentAction.ToString() : "<none>");

			if (m_currentAssignment == null)
			{
				D("ActionStarted: no assignment, so not for me");
				return;
			}

			if (m_currentAssignment.CurrentAction == null)
			{
				D("ActionStarted: action started by someone else, cancel our current assignment");
				m_currentAssignment.Abort();
				m_currentAssignment = null;
				return;
			}

			if (m_currentAssignment.CurrentAction.MagicNumber != change.Action.MagicNumber)
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
				m_currentAssignment != null ? m_currentAssignment.ToString() : "<none>",
				m_currentAssignment != null && m_currentAssignment.CurrentAction != null ? m_currentAssignment.CurrentAction.ToString() : "<none>");

			Debug.Assert(this.Worker.HasAction);
			Debug.Assert(e.ActionXXX.MagicNumber == this.Worker.CurrentAction.MagicNumber);

			if (m_currentAssignment == null)
			{
				D("ActionProgress: no assignment, so not for me");
				return;
			}

			if (e.State == ActionState.Abort && m_currentAssignment.CurrentAction != null &&
				m_currentAssignment.CurrentAction.MagicNumber != e.ActionXXX.MagicNumber)
			{
				D("ActionProgress: cancel event for action not started by us, ignore");
				return;
			}

			if (m_currentAssignment.CurrentAction == null)
			{
				throw new NotImplementedException("implement cancel work");
			}

			// does the action originate from us?
			if (m_currentAssignment.CurrentAction.MagicNumber != e.ActionXXX.MagicNumber)
			{
				throw new NotImplementedException("implement cancel work");
			}

			Debug.Assert(e.ObjectID == this.Worker.ObjectID);

			var progress = m_currentAssignment.ActionProgress(e);

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
					D("ActionProgress: {0} in {1}", progress, m_currentAssignment);
					m_currentAssignment = null;
					break;
			}
		}
	}
}
