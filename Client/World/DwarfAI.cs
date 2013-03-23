using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;
using Dwarrowdelf.AI;
using System.Diagnostics;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Client
{
	[SaveGameObject]
	public sealed class DwarfAI : AssignmentAI, IJobObserver
	{
		enum DwarfState
		{
			None,
			Working,
			Fighting,
		}

		[SaveGameProperty]
		DwarfState State { get; set; }

		public JobManager JobManager { get; set; }

		[SaveGameProperty]
		ILivingObject m_target;

		public DwarfAI(ILivingObject worker, int playerID)
			: base(worker, playerID)
		{
			this.State = DwarfState.Working;
		}

		DwarfAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public override string Name { get { return "DwarfAI"; } }

		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			switch (this.State)
			{
				case DwarfState.Working:
					{
						Debug.Assert(m_target == null);

						m_target = AIHelpers.FindNearestEnemy(this.Worker, LivingCategory.Monster);

						if (m_target == null)
						{
							// continue doing work

							if (this.CurrentAssignment != null)
								return this.CurrentAssignment;

							return this.JobManager.FindAssignment(this.Worker);
						}

						this.State = DwarfState.Fighting;

						trace.TraceInformation("Found target: {0}", m_target);

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

				case DwarfState.Fighting:
					if (m_target.IsDestructed)
						return null;

					Debug.Assert(this.CurrentAssignment != null);

					trace.TraceInformation("Continue attacking: {0}", m_target);

					return this.CurrentAssignment;

				default:
					throw new Exception();
			}
		}

		#region IJobObserver Members

		public void OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			m_target = null;
			this.State = DwarfState.Working;
		}

		#endregion
	}
}
