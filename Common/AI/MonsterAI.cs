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
	public sealed class MonsterAI : AssignmentAI, IJobObserver
	{
		ILivingObject m_target;

		MonsterAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public MonsterAI(ILivingObject ob, byte aiID)
			: base(ob, aiID)
		{
			trace = new MyTraceSource("Dwarrowdelf.MonsterAI", String.Format("AI {0}", this.Worker));
		}

		[OnSaveGamePostDeserialization]
		void OnPostDeserialization()
		{
			trace = new MyTraceSource("Dwarrowdelf.MonsterAI", String.Format("AI {0}", this.Worker));
		}

		new MyTraceSource trace;
		public override string Name { get { return "MonsterAI"; } }

		// return new or current assignment, or null to cancel current assignment, or do nothing is no current assignment
		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			if (priority == ActionPriority.Idle)
				return this.CurrentAssignment;

			if (m_target == null)
			{
				m_target = AIHelpers.FindNearestEnemy(this.Worker, LivingCategory.Civilized);

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
						trace.TraceVerbose("Continue patrolling");
						return this.CurrentAssignment;
					}
				}

				trace.TraceInformation("Found target: {0}", m_target);
			}

			Debug.Assert(m_target != null);

			if (this.CurrentAssignment == null || (this.CurrentAssignment is AttackAssignment) == false)
			{
				trace.TraceInformation("Start attacking: {0}", m_target);

				var assignment = new AttackAssignment(this, m_target);
				return assignment;
			}
			else
			{
				trace.TraceInformation("Continue attacking: {0}", m_target);
				return this.CurrentAssignment;
			}
		}

		#region IJobObserver Members

		public void OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			trace.TraceInformation("Attack finished: {0} ({1})", m_target, status);

			m_target = null;
		}

		#endregion
	}
}
