using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Server
{
	class DwarfAI : AssignmentAI
	{
		bool m_priorityAction;

		public DwarfAI(Living ob)
			: base(ob)
		{
		}

		// return new or current assignment, or null to cancel current assignment, or do nothing is no current assignment
		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			var worker = (Living)this.Worker;

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

				if (hasAssignment)
					return this.CurrentAssignment;

				return new Jobs.AssignmentGroups.LoiterJob(null, priority, worker.Environment);
			}
			else
			{
				throw new Exception();
			}
		}

		IAssignment CreateFoodAssignmentIfNeeded(Living worker, ActionPriority priority)
		{
			if (priority == ActionPriority.High && worker.FoodFullness > 50)
				return null;

			if (priority == ActionPriority.Idle && worker.FoodFullness > 300)
				return null;

			Debug.Assert(m_consumeObject == null);

			ItemObject ob = null;
			var env = worker.Environment;

			ob = env.Objects()
				.OfType<ItemObject>()
				.Where(o => o.ReservedBy == null && o.NutritionalValue > 0)
				.OrderBy(o => (o.Location - worker.Location).ManhattanLength)
				.FirstOrDefault();

			if (ob != null)
			{
				m_priorityAction = true;
				ob.ReservedBy = worker;
				var job = new Jobs.AssignmentGroups.MoveConsumeJob(null, priority, ob);
				job.StateChanged += OnConsumeJobStateChanged;
				m_consumeObject = ob;
				return job;
			}

			return null;
		}

		IAssignment CreateDrinkAssignmentIfNeeded(Living worker, ActionPriority priority)
		{
			if (priority == ActionPriority.High && worker.WaterFullness > 50)
				return null;

			if (priority == ActionPriority.Idle && worker.WaterFullness > 300)
				return null;

			Debug.Assert(m_consumeObject == null);

			ItemObject ob = null;
			var env = worker.Environment;

			ob = env.Objects()
				.OfType<ItemObject>()
				.Where(o => o.ReservedBy == null && o.RefreshmentValue > 0)
				.OrderBy(o => (o.Location - worker.Location).ManhattanLength)
				.FirstOrDefault();

			if (ob != null)
			{
				m_priorityAction = true;
				ob.ReservedBy = worker;
				var job = new Jobs.AssignmentGroups.MoveConsumeJob(null, priority, ob);
				job.StateChanged += OnConsumeJobStateChanged;
				m_consumeObject = ob;
				return job;
			}

			return null;
		}

		ItemObject m_consumeObject;
		void OnConsumeJobStateChanged(IJob job, JobState state)
		{
			// XXX ob's ReservedBy should probably be cleared elsewhere
			m_consumeObject.ReservedBy = null;
			m_consumeObject = null;
			job.StateChanged -= OnConsumeJobStateChanged;
			m_priorityAction = false;
		}
	}
}
