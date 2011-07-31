using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;
using Dwarrowdelf.Jobs.Assignments;
using System.Diagnostics;
using Dwarrowdelf.Jobs.AssignmentGroups;
using Dwarrowdelf.AI;

namespace Dwarrowdelf.Client
{
	[SaveGameObject]
	public class FighterAI : AssignmentAI
	{
		[SaveGameProperty]
		ILiving m_target;

		[SaveGameProperty]
		List<IntPoint3D> m_patrolRoute;

		public FighterAI(ILiving worker)
			: base(worker)
		{
			this.OnDuty = true;
			m_patrolRoute = new List<IntPoint3D>();

			// XXX
			m_patrolRoute.Add(new IntPoint3D(2, 2, 2));
			m_patrolRoute.Add(new IntPoint3D(50, 2, 2));
			m_patrolRoute.Add(new IntPoint3D(50, 50, 2));
			m_patrolRoute.Add(new IntPoint3D(2, 50, 2));
		}

		FighterAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		[SaveGameProperty]
		public bool OnDuty { get; set; }

		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			if (this.OnDuty == false)
				return null;

			if (m_target == null)
			{
				m_target = AIHelpers.FindNearbyEnemy(this.Worker, LivingClass.Carnivore | LivingClass.Monster);

				if (m_target == null)
				{
					// continue patrolling
					if (this.CurrentAssignment == null || (this.CurrentAssignment is PatrolAssignment) == false)
					{
						trace.TraceInformation("Start patrolling");
						return new PatrolAssignment(null, ActionPriority.Normal, this.Worker.Environment, m_patrolRoute.ToArray());
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

				var assignment = new AttackAssignment(null, ActionPriority.Normal, m_target);
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

#if asd
		Living FindNearbyEnemy()
		{
			// XXX
			var env = this.Worker.Environment;
			var center = this.Worker.Location;

			const int r = 20;

			int maxSide = 2 * r + 1;

			for (int s = 3; s < maxSide; s += 2)
			{
				var rect = new IntRect(center.X - s / 2, center.Y - s / 2, s, s);
				var range = rect.Perimeter();

				foreach (var p2d in range)
				{
					var p = new IntPoint3D(p2d, center.Z);

					if (!env.Contains(p))
						continue;

					var obs = env.GetContents(p).OfType<Living>();
					obs = obs.Where(o => !o.IsControllable); // XXX

					var ob = obs.FirstOrDefault();

					if (ob != null)
						return ob;
				}
			}

			return null;
		}
#endif
	}
}
