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
	public enum HervivoreAIState
	{
		None,
		Grazing,
		Fleeing,
	}

	[SaveGameObjectByRef]
	public sealed class HerbivoreAI : AssignmentAI, IJobObserver
	{
		[SaveGameProperty]
		Group m_group;

		[SaveGameProperty]
		public HervivoreAIState State { get; private set; }

		[SaveGameProperty]
		FleeMoveAssignment m_fleeAssignment;

		public Group Group
		{
			get { return m_group; }

			set
			{
				if (m_group != null)
					m_group.RemoveMember(this);

				m_group = value;

				if (m_group != null)
					m_group.AddMember(this);
			}
		}

		public HerbivoreAI(ILivingObject ob)
			: base(ob)
		{
			this.State = HervivoreAIState.Grazing;
		}

		HerbivoreAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public override string Name { get { return "HerbivoreAI"; } }

		// return new or current assignment, or null to cancel current assignment, or do nothing is no current assignment
		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			if (priority == ActionPriority.High)
				return this.CurrentAssignment;

			var worker = this.Worker;

			bool hasAssignment = this.CurrentAssignment != null;
			bool hasOtherAssignment = this.CurrentAssignment == null && this.Worker.HasAction;

			if (hasOtherAssignment)
				return null;

			switch (this.State)
			{
				case HervivoreAIState.Grazing:
					{
						var enemies = AIHelpers.FindEnemies(worker, LivingCategory.Carnivore | LivingCategory.Civilized | LivingCategory.Monster);

						if (enemies.Any())
						{
							var fleeVector = GetFleeVector(enemies);

							trace.TraceInformation("Changing to Flee state, v = {0}", fleeVector);

							this.State = HervivoreAIState.Fleeing;
							m_fleeAssignment = new FleeMoveAssignment(this);
							m_fleeAssignment.SetFleeVector(fleeVector);
							return m_fleeAssignment;
						}

						if (hasAssignment)
							return this.CurrentAssignment;

						return new Dwarrowdelf.Jobs.Assignments.GrazeMoveAssignment(this, this.Group);
					}

				case HervivoreAIState.Fleeing:
					{
						var enemies = AIHelpers.FindEnemies(worker, LivingCategory.Carnivore | LivingCategory.Civilized | LivingCategory.Monster);

						if (enemies.Any())
						{
							var fleeVector = GetFleeVector(enemies);

							trace.TraceInformation("Updating fleevector: {0}", fleeVector);
							m_fleeAssignment.SetFleeVector(fleeVector);
							return m_fleeAssignment;
						}

						m_fleeAssignment.Abort();

						trace.TraceInformation("Changing to Graze state");

						this.State = HervivoreAIState.Grazing;
						return new Dwarrowdelf.Jobs.Assignments.GrazeMoveAssignment(this, this.Group);
					}

				default:
					throw new Exception();
			}
		}

		DoubleVector3 GetFleeVector(IEnumerable<ILivingObject> enemies)
		{
			var fleeVector = enemies.Aggregate(new DoubleVector3(),
				(accu, enemy) =>
				{
					var v = new DoubleVector3(this.Worker.Location - enemy.Location);

					// XXX if null vector, flee up. (which probably leads to fleeing in some parallel direction).
					if (v.IsNull)
						return accu + new DoubleVector3(0, 0, 1);

					v = v * 100 / v.SquaredLength;
					return accu + v;
				});

			return fleeVector;
		}

		#region IJobObserver Members

		public void OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			if (m_fleeAssignment != null && job == m_fleeAssignment)
				m_fleeAssignment = null;
		}

		#endregion
	}
}
