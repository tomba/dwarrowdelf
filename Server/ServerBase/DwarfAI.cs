using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	class DwarfAI : Jobs.AssignmentAI
	{
		//Living Worker { get; set; }
		Random m_random;
		bool m_priorityAction;

		public DwarfAI(Living ob)
			: base(ob)
		{
			m_random = new Random();
			//this.Worker = ob;
		}

		protected override bool CheckForAbortOtherAction(ActionPriority priority)
		{
			var worker = (Living)this.Worker;

			if (priority == ActionPriority.High)
			{
				return false;
			}
			else
			{
				if (m_priorityAction)
					return false;

				if (worker.FoodFullness < 200)
					return true;

				return false;
			}
		}

		protected override bool CheckForAbortOurAssignment(ActionPriority priority)
		{
			var worker = (Living)this.Worker;

			if (priority == ActionPriority.High)
			{
				return false;
			}
			else
			{
				if (m_priorityAction)
					return false;

				if (worker.FoodFullness < 200)
					return true;

				return false;
			}
		}

		protected override Jobs.IAssignment GetAssignment(ILiving _worker, ActionPriority priority)
		{
			var worker = (Living)_worker;

			/*
			if (priority == ActionPriority.High)
			{
				if (this.Worker.World.TickNumber % 20 == 0)
					return new Jobs.Assignments.WaitAssignment(null, priority, 8);
				else
					return null;
			}
			 */
			/*
			return new Jobs.WaitAssignment(null, priority, 4);
			*/

			if (priority == ActionPriority.High)
			{
				return null;
			}
			else
			{
				if (worker.FoodFullness < 200)
				{
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
						job.StateChanged += OnJobStateChanged;
						return job;
					}
				}

				return new Jobs.AssignmentGroups.LoiterJob(null, priority, worker.Environment);
			}
		}

		void OnJobStateChanged(Jobs.IJob job, Jobs.JobState state)
		{
			job.StateChanged -= OnJobStateChanged;
			m_priorityAction = false;
		}

#if asd
		public GameAction ActionRequired(ActionPriority priority)
		{
			if (priority == ActionPriority.Idle)
			{
				if (this.Worker.HasAction && this.Worker.CurrentAction.Priority >= priority)
					return null;

				if (this.Worker.HasAction)
					this.Worker.CancelAction();
				var a = GetNewAction(priority);

				return a;
			}
			else
				return null;
		}

		public void ActionProgress(ActionProgressChange e)
		{
		}

		GameAction GetNewAction(ActionPriority priority)
		{
			GameAction action;

			if (m_random.Next(4) == 0)
				action = new WaitAction(m_random.Next(3) + 1, priority);
			else
			{
				IntVector v = new IntVector(1, 1);
				v = v.Rotate(45 * m_random.Next(8));
				Direction dir = v.ToDirection();

				if (dir == Direction.None)
					throw new Exception();

				action = new MoveAction(dir, priority);
			}

			return action;
		}
#endif

	}
}
