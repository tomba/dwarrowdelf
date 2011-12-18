using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Dwarrowdelf;
using Dwarrowdelf.Jobs;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.AI
{
	[SaveGameObjectByRef]
	public class MonsterAI : AssignmentAI
	{
		ILivingObject m_target;

		MonsterAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public MonsterAI(ILivingObject ob)
			: base(ob)
		{
		}

		public override string Name { get { return "MonsterAI"; } }

		// return new or current assignment, or null to cancel current assignment, or do nothing is no current assignment
		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			if (priority == ActionPriority.Idle)
				return this.CurrentAssignment;

			if (m_target == null)
			{
				m_target = AIHelpers.FindNearbyEnemy(this.Worker, LivingCategory.Civilized);

				if (m_target == null)
				{
					// continue patrolling
					if (this.CurrentAssignment == null || (this.CurrentAssignment is RandomMoveAssignment) == false)
					{
						trace.TraceInformation("Start random move");
						return new RandomMoveAssignment(null);
					}
					else
					{
						trace.TraceInformation("Continue patrolling");
						return this.CurrentAssignment;
					}
				}

				trace.TraceInformation("Found target");
			}

			Debug.Assert(m_target != null);

			if (this.CurrentAssignment == null || (this.CurrentAssignment is AttackAssignment) == false)
			{
				trace.TraceInformation("Start attacking");

				var assignment = new AttackAssignment(null, m_target);
				assignment.StatusChanged += OnAttackStatusChanged;
				return assignment;
			}
			else
			{
				trace.TraceInformation("Continue attacking");
				return this.CurrentAssignment;
			}
		}

		void OnAttackStatusChanged(IJob job, JobStatus status)
		{
			trace.TraceInformation("Attack finished: {0}", status);

			job.StatusChanged -= OnAttackStatusChanged;
			m_target = null;
		}
	}
}
