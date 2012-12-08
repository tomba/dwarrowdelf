using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Dwarrowdelf;
using Dwarrowdelf.Jobs;
using Dwarrowdelf.AI;
using Dwarrowdelf.Jobs.Assignments;
using Dwarrowdelf.Jobs.AssignmentGroups;

namespace Dwarrowdelf.Server.Fortress
{
	[SaveGameObjectByRef]
	sealed class DwarfAI : AssignmentAI, IJobObserver
	{
		[SaveGameProperty]
		bool m_priorityAction;

		[SaveGameProperty]
		EnvObserver m_envObserver;

		public DwarfAI(ILivingObject ob, EnvObserver envObserver, byte aiID)
			: base(ob, aiID)
		{
			m_envObserver = envObserver;
		}

		DwarfAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public override string Name { get { return "DwarfAI"; } }

		// return new or current assignment, or null to cancel current assignment, or do nothing is no current assignment
		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			var worker = this.Worker;

			bool hasAssignment = this.CurrentAssignment != null;
			bool hasOtherAssignment = this.CurrentAssignment == null && this.Worker.HasAction;

			if (priority == ActionPriority.High)
			{
				if (m_priorityAction)
					return this.CurrentAssignment;

				var assignment = CreateFoodAssignmentIfNeeded(worker, priority);
				if (assignment != null)
					return assignment;

				assignment = CreateDrinkAssignmentIfNeeded(worker, priority);
				if (assignment != null)
					return assignment;

				return this.CurrentAssignment;
			}
			else if (priority == ActionPriority.Idle)
			{
				if (hasOtherAssignment)
					return null;

				if (m_priorityAction)
					return this.CurrentAssignment;

				var assignment = CreateFoodAssignmentIfNeeded(worker, priority);
				if (assignment != null)
					return assignment;

				assignment = CreateDrinkAssignmentIfNeeded(worker, priority);
				if (assignment != null)
					return assignment;

				assignment = CreateSleepAssignmentIfNeeded(worker, priority);
				if (assignment != null)
					return assignment;

				// loiter around


				if (m_envObserver.Contains(worker.Location))
				{
					if (hasAssignment && this.CurrentAssignment is RandomMoveAssignment)
						return this.CurrentAssignment;
					else
						return new RandomMoveAssignment(this);
				}
				else
				{
					var c = m_envObserver.Center;

					if (hasAssignment && c.HasValue && this.CurrentAssignment is MoveAssignment)
						return this.CurrentAssignment;
					else
					{
						if (c.HasValue)
							return new MoveAssignment(this, worker.Environment, c.Value, DirectionSet.Planar | DirectionSet.Exact);
						else
							return new RandomMoveAssignment(this);
					}
				}
			}
			else
			{
				throw new Exception();
			}
		}

		IAssignment CreateFoodAssignmentIfNeeded(ILivingObject worker, ActionPriority priority)
		{
			if (priority == ActionPriority.High && worker.Hunger < 500)
				return null;

			if (priority == ActionPriority.Idle && worker.Hunger < 200)
				return null;

			var env = worker.Environment;

			var ob = env.Inventory
				.OfType<IItemObject>()
				.Where(o => o.IsReserved == false && o.NutritionalValue > 0)
				.OrderBy(o => (o.Location - worker.Location).ManhattanLength)
				.FirstOrDefault();

			if (ob != null)
			{
				m_priorityAction = true;
				var job = new MoveConsumeAssignment(this, ob);
				ob.ReservedBy = this;
				return job;
			}

			return null;
		}

		IAssignment CreateDrinkAssignmentIfNeeded(ILivingObject worker, ActionPriority priority)
		{
			if (priority == ActionPriority.High && worker.Thirst < 500)
				return null;

			if (priority == ActionPriority.Idle && worker.Thirst < 200)
				return null;

			var env = worker.Environment;

			var ob = env.Inventory
				.OfType<IItemObject>()
				.Where(o => o.IsReserved == false && o.RefreshmentValue > 0)
				.OrderBy(o => (o.Location - worker.Location).ManhattanLength)
				.FirstOrDefault();

			if (ob != null)
			{
				m_priorityAction = true;
				var job = new MoveConsumeAssignment(this, ob);
				ob.ReservedBy = this;
				return job;
			}

			return null;
		}

		IAssignment CreateSleepAssignmentIfNeeded(ILivingObject worker, ActionPriority priority)
		{
			if (priority == ActionPriority.High && worker.Exhaustion < 500)
				return null;

			if (priority == ActionPriority.Idle && worker.Exhaustion < 100)
				return null;

			var env = worker.Environment;

			var ob = env.Inventory
				.OfType<IItemObject>()
				.Where(o => o.ItemID == ItemID.Bed)
				.Where(o => o.IsReserved == false && o.IsInstalled)
				.OrderBy(o => (o.Location - worker.Location).ManhattanLength)
				.FirstOrDefault();

			if (ob != null)
			{
				m_priorityAction = true;
				var job = new MoveSleepAssignment(this, ob);
				ob.ReservedBy = this;
				return job;
			}

			return null;
		}

		protected override void JobStatusChangedOverride(IJob job, JobStatus status)
		{
			// XXX hacksor. Will get called when loiterjob is aborted, because we're gonna add an eat job. 
			// and at that point we've marked the consumeobject and priorityaction

			if (!(job is MoveConsumeAssignment) && !(job is MoveSleepAssignment))
				return;

			if (m_priorityAction)
			{
				m_priorityAction = false;
			}
		}

		#region IJobObserver Members

		public void OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			var j = job as Dwarrowdelf.Jobs.AssignmentGroups.MoveConsumeAssignment;

			if (j != null)
			{
				if (!j.Item.IsDestructed)
				{
					Debug.Assert(j.Item.ReservedBy == this);
					j.Item.ReservedBy = null;
				}
			}

			var j2 = job as Dwarrowdelf.Jobs.AssignmentGroups.MoveSleepAssignment;

			if (j2 != null)
			{
				if (!j2.Bed.IsDestructed)
				{
					Debug.Assert(j2.Bed.ReservedBy == this);
					j2.Bed.ReservedBy = null;
				}
			}
		}

		#endregion
	}
}
