using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	abstract class VisionTrackerBase : IVisionTracker
	{
		public virtual void AddLiving(LivingObject living) { }
		public virtual void RemoveLiving(LivingObject living) { }

		public abstract bool Sees(IntPoint3 p);
	}

	sealed class AdminVisionTracker : VisionTrackerBase
	{
		public static AdminVisionTracker Tracker = new AdminVisionTracker();

		public override bool Sees(IntPoint3 p)
		{
			return true;
		}

		public override void AddLiving(LivingObject living)
		{
		}

		public override void RemoveLiving(LivingObject living)
		{
		}
	}

	sealed class AllVisibleVisionTracker : VisionTrackerBase
	{
		Player m_player;
		EnvironmentObject m_environment;

		int m_livingCount;

		public AllVisibleVisionTracker(Player player, EnvironmentObject env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.AllVisible);

			m_player = player;
			m_environment = env;
		}

		public override void AddLiving(LivingObject living)
		{
			if (m_livingCount == 0)
				m_environment.SendTo(m_player, ObjectVisibility.Public);

			m_livingCount++;
		}

		public override void RemoveLiving(LivingObject living)
		{
			m_livingCount--;
			Debug.Assert(m_livingCount >= 0);
		}

		public override bool Sees(IntPoint3 p)
		{
			return m_environment.Contains(p);
		}
	}
}
