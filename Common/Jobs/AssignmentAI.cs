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
					m_currentAssignment.StateChanged -= OnJobStateChanged;

				m_currentAssignment = value;

				if (m_currentAssignment != null)
					m_currentAssignment.StateChanged += OnJobStateChanged;

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
		void D(string format, params object[] args)
		{
			Debug.Print("[AI {0}]: {1}", this.Worker, String.Format(format, args));
		}

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

			while (true)
			{
				var assignment = GetNewOrCurrentAssignment(priority);
				var oldAssignment = this.CurrentAssignment;

				if (assignment != oldAssignment)
				{
					if (oldAssignment != null)
					{
						D("DecideAction: Aborting current assignment {0}", oldAssignment);
						oldAssignment.Abort();
					}

					this.CurrentAssignment = assignment;
				}

				// TODO: if the assignment was just aborted, we should abort possibly ongoing action too.
				// no assignment, no action
				if (assignment == null)
				{
					D("DecideAction: No assignment");
					return null;
				}


				// are we doing an assignment for another priority level?
				if (assignment.Priority != priority)
				{
					Debug.Assert(assignment.Worker == this.Worker);
					D("DecideAction: Already doing an assignment for different priority level");
					return null;
				}


				// new assignment?
				if (assignment.Worker == null)
				{
					D("DecideAction: New assignment {0}", assignment);

					var assignState = assignment.Assign(this.Worker);

					if (assignState != JobState.Ok)
						continue;
				}
				// are we already doing an action for this assignment?
				else if (assignment.CurrentAction != null)
				{
					D("DecideAction: already doing an action");
					return null;
				}



				var state = assignment.PrepareNextAction();

				if (state == JobState.Ok)
				{
					var action = assignment.CurrentAction;
					if (action == null)
						throw new Exception();

					D("DecideAction: new action {0}", action);
					return action;
				}

				D("DecideAction: {0} in {1}, looking for new assignment", state, assignment);

				// loop again
			}
		}

		/// <summary>
		/// return new or current assignment, or null to cancel current assignment, or do nothing is no current assignment
		/// </summary>
		/// <param name="priority"></param>
		/// <returns></returns>
		protected abstract IAssignment GetNewOrCurrentAssignment(ActionPriority priority);


		void OnJobStateChanged(IJob job, JobState state)
		{
			Debug.Assert(job == this.CurrentAssignment);

			Debug.Assert(job.JobState != JobState.Ok);
			this.CurrentAssignment = null;
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
			var assignment = this.CurrentAssignment;

			D("ActionProgress({0}, State {1}): Worker.Action = {2}, CurrentAssignment {3}, CurrentAssignment.Action = {4}",
				e.ActionXXX, e.State,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				assignment != null ? assignment.ToString() : "<none>",
				assignment != null && assignment.CurrentAction != null ? assignment.CurrentAction.ToString() : "<none>");

			Debug.Assert(this.Worker.HasAction);
			Debug.Assert(e.ActionXXX.MagicNumber == this.Worker.CurrentAction.MagicNumber);

			if (assignment == null)
			{
				D("ActionProgress: no assignment, so not for me");
				return;
			}

			if (e.State == ActionState.Abort && assignment.CurrentAction != null &&
				assignment.CurrentAction.MagicNumber != e.ActionXXX.MagicNumber)
			{
				D("ActionProgress: cancel event for action not started by us, ignore");
				return;
			}

			if (assignment.CurrentAction == null)
			{
				// XXX this can happen when doing a multi-turn action, and the user aborts the action. 
				throw new NotImplementedException("implement cancel work");
			}

			// does the action originate from us?
			if (assignment.CurrentAction.MagicNumber != e.ActionXXX.MagicNumber)
			{
				throw new NotImplementedException("implement cancel work");
			}

			Debug.Assert(e.ObjectID == this.Worker.ObjectID);

			var state = assignment.ActionProgress(e);

			D("ActionProgress: {0} in {1}", state, assignment);
		}
	}
}
