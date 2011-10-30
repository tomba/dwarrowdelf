using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Dwarrowdelf.Jobs;
using System.Diagnostics;

namespace Dwarrowdelf.AI
{
	/// <summary>
	/// abstract AI that handles Assignments
	/// </summary>
	[SaveGameObjectByRef]
	public abstract class AssignmentAI : IAI
	{
		[SaveGameProperty]
		public ILiving Worker { get; private set; }

		[SaveGameProperty("NeedToAbort")]
		bool m_needToAbort;

		[SaveGameProperty]
		IAssignment m_currentAssignment;
		[SaveGameProperty]
		ActionPriority m_currentPriority;

		public IAssignment CurrentAssignment { get { return m_currentAssignment; } }

		public ActionPriority CurrentPriority { get { return m_currentPriority; } }

		void SetCurrentAssignment(IAssignment assignment, ActionPriority priority)
		{
			if (m_currentAssignment != null)
			{
				if (this.Worker.HasAction)
					m_needToAbort = true;	// XXX what if worker has high priority server action?
				m_currentAssignment.StatusChanged -= OnJobStatusChanged;
			}

			m_currentAssignment = assignment;
			m_currentPriority = priority;

			if (m_currentAssignment != null)
				m_currentAssignment.StatusChanged += OnJobStatusChanged;

			if (AssignmentChanged != null)
				AssignmentChanged(assignment);
		}

		void ClearCurrentAssignment()
		{
			SetCurrentAssignment(null, ActionPriority.Undefined);
		}

		public event Action<IAssignment> AssignmentChanged;

		protected MyTraceSource trace;

		protected AssignmentAI(SaveGameContext ctx)
		{
			if (m_currentAssignment != null)
				m_currentAssignment.StatusChanged += OnJobStatusChanged;
		}

		protected AssignmentAI(ILiving worker)
		{
			this.Worker = worker;
			trace = new MyTraceSource("Dwarrowdelf.AssignmentAI", String.Format("AI {0}", this.Worker));
		}

		[OnSaveGamePostDeserialization]
		void OnPostDeserialization()
		{
			trace = new MyTraceSource("Dwarrowdelf.AssignmentAI", String.Format("AI {0}", this.Worker));
		}

		/// <summary>
		/// return New or current GameAction, possibly overriding the current action, or null to abort the current action
		/// </summary>
		/// <param name="priority"></param>
		/// <returns></returns>
		public GameAction DecideAction(ActionPriority priority)
		{
			trace.TraceVerbose("DecideAction({0}): Worker.Action = {1}, CurrentAssignment {2}, CurrentAssignment.Action = {3}",
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

			if (this.Worker.HasAction && this.Worker.ActionPriority > priority)
			{
				trace.TraceVerbose("DecideAction: worker already doing higher priority action");
				return this.Worker.CurrentAction;
			}

			var needToAbort = m_needToAbort;
			m_needToAbort = false;

			int loops = 0;

			while (true)
			{
				if (loops++ > 10)
				{
					trace.TraceWarning("Failed to assign job in 10 tries, aborting");
					return this.Worker.CurrentAction;
				}

				var assignment = GetNewOrCurrentAssignment(priority);

				var oldAssignment = this.CurrentAssignment;

				if (assignment == null)
				{
					trace.TraceVerbose("DecideAction: No assignment");

					if (oldAssignment != null)
					{
						trace.TraceVerbose("DecideAction: Aborting current assignment {0}", oldAssignment);
						oldAssignment.Abort();
					}

					ClearCurrentAssignment();

					return needToAbort ? null : this.Worker.CurrentAction;
				}

				// are we doing this assignment for another priority level?
				if (assignment == oldAssignment && this.CurrentPriority != priority)
				{
					Debug.Assert(assignment == oldAssignment);
					Debug.Assert(assignment.Worker == this.Worker);
					trace.TraceVerbose("DecideAction: Already doing an assignment for different priority level");
					return this.Worker.CurrentAction;
				}

				// new assignment?
				if (assignment != oldAssignment)
				{
					trace.TraceVerbose("DecideAction: New assignment {0}", assignment);

					Debug.Assert(assignment.IsAssigned == false);

					assignment.Assign(this.Worker);
					Debug.Assert(assignment.Status == JobStatus.Ok);

					if (oldAssignment != null)
					{
						trace.TraceVerbose("DecideAction: Aborting current assignment {0}", oldAssignment);
						oldAssignment.Abort();
					}

					SetCurrentAssignment(assignment, priority);
				}

				Debug.Assert(this.CurrentAssignment == assignment);

				// are we already doing an action for this assignment?
				if (assignment.CurrentAction != null)
				{
					trace.TraceVerbose("DecideAction: already doing an action");
					//Debug.Assert(assignment.CurrentAction == this.Worker.CurrentAction);
					return this.Worker.CurrentAction;
				}


				var state = assignment.PrepareNextAction();

				if (state == JobStatus.Ok)
				{
					var action = assignment.CurrentAction;
					if (action == null)
						throw new Exception();

					trace.TraceVerbose("DecideAction: new action {0}", action);
					return action;
				}

				trace.TraceVerbose("DecideAction: {0} in {1}, looking for new assignment", state, assignment);

				// loop again
			}
		}

		/// <summary>
		/// return new or current assignment, or null to cancel current assignment, or do nothing if no current assignment
		/// </summary>
		/// <param name="priority"></param>
		/// <returns></returns>
		protected abstract IAssignment GetNewOrCurrentAssignment(ActionPriority priority);


		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			Debug.Assert(job == this.CurrentAssignment);

			JobStatusChangedOverride(job, status);

			Debug.Assert(job.Status != JobStatus.Ok);
			ClearCurrentAssignment();
		}

		protected virtual void JobStatusChangedOverride(IJob job, JobStatus status) { }

		public void ActionStarted(ActionStartedChange change)
		{
			trace.TraceVerbose("ActionStarted({0}): Worker.Action = {1}, CurrentAssignment {2}, CurrentAssignment.Action = {3}",
				change.Action,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				this.CurrentAssignment != null ? this.CurrentAssignment.ToString() : "<none>",
				this.CurrentAssignment != null && this.CurrentAssignment.CurrentAction != null ? this.CurrentAssignment.CurrentAction.ToString() : "<none>");

			if (this.CurrentAssignment == null)
			{
				trace.TraceVerbose("ActionStarted: no assignment, so not for me");
				return;
			}

			if (this.CurrentAssignment.CurrentAction == null)
			{
				trace.TraceVerbose("ActionStarted: action started by someone else, cancel our current assignment");
				this.CurrentAssignment.Abort();
				ClearCurrentAssignment();
				return;
			}

			if (this.CurrentAssignment.CurrentAction.MagicNumber != change.Action.MagicNumber)
			{
				trace.TraceVerbose("ActionStarted: action started by someone else, cancel our current assignment");
				throw new Exception();
			}

			// otherwise, it's an action started by us, all ok.
		}

		public void ActionProgress(ActionProgressChange e)
		{
			var assignment = this.CurrentAssignment;

			trace.TraceVerbose("ActionProgress({0}): Worker.Action = {1}, CurrentAssignment {2}, CurrentAssignment.Action = {3}",
				e.MagicNumber,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				assignment != null ? assignment.ToString() : "<none>",
				assignment != null && assignment.CurrentAction != null ? assignment.CurrentAction.ToString() : "<none>");

			Debug.Assert(this.Worker.HasAction);
			Debug.Assert(e.MagicNumber == this.Worker.CurrentAction.MagicNumber);

			if (assignment == null)
			{
				trace.TraceVerbose("ActionProgress: no assignment, so not for me");
				return;
			}

			if (assignment.CurrentAction == null)
			{
				// XXX this can happen when doing a multi-turn action, and the user aborts the action. 
				throw new NotImplementedException("implement cancel work");
			}

			// does the action originate from us?
			if (assignment.CurrentAction.MagicNumber != e.MagicNumber)
			{
				throw new NotImplementedException("implement cancel work");
			}

			Debug.Assert(e.ObjectID == this.Worker.ObjectID);

			var state = assignment.ActionProgress();

			trace.TraceVerbose("ActionProgress: {0} in {1}", state, assignment);
		}

		public void ActionDone(ActionDoneChange e)
		{
			var assignment = this.CurrentAssignment;

			trace.TraceVerbose("ActionDone({0}, State {1}): Worker.Action = {2}, CurrentAssignment {3}, CurrentAssignment.Action = {4}",
				e.MagicNumber, e.State,
				this.Worker.CurrentAction != null ? this.Worker.CurrentAction.ToString() : "<none>",
				assignment != null ? assignment.ToString() : "<none>",
				assignment != null && assignment.CurrentAction != null ? assignment.CurrentAction.ToString() : "<none>");

			Debug.Assert(this.Worker.HasAction);
			Debug.Assert(e.MagicNumber == this.Worker.CurrentAction.MagicNumber);

			if (assignment == null)
			{
				trace.TraceVerbose("ActionDone: no assignment, so not for me");
				return;
			}

			if (e.State == ActionState.Abort && assignment.CurrentAction != null &&
				assignment.CurrentAction.MagicNumber != e.MagicNumber)
			{
				trace.TraceVerbose("ActionDone: cancel event for action not started by us, ignore");
				return;
			}

			if (assignment.CurrentAction == null)
			{
				// XXX this can happen when doing a multi-turn action, and the user aborts the action. 
				throw new NotImplementedException("implement cancel work");
			}

			// does the action originate from us?
			if (assignment.CurrentAction.MagicNumber != e.MagicNumber)
			{
				throw new NotImplementedException("implement cancel work");
			}

			Debug.Assert(e.ObjectID == this.Worker.ObjectID);

			var state = assignment.ActionDone(e.State);

			trace.TraceVerbose("ActionDone: {0} in {1}", state, assignment);
		}
	}
}
