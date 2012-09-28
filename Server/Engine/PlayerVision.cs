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
		public abstract void Start();
		public abstract void Stop();
		public abstract bool Sees(IntPoint3 p);
		public virtual void HandleNewControllable(LivingObject living) { } // XXX update vision map
	}

	sealed class AdminVisionTracker : VisionTrackerBase
	{
		public static AdminVisionTracker Tracker = new AdminVisionTracker();

		public override bool Sees(IntPoint3 p)
		{
			return true;
		}

		public override void Start()
		{
		}

		public override void Stop()
		{
		}
	}

	sealed class AllVisibleVisionTracker : VisionTrackerBase
	{
		Player m_player;
		EnvironmentObject m_environment;

		public AllVisibleVisionTracker(Player player, EnvironmentObject env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.AllVisible);

			m_player = player;
			m_environment = env;
		}

		public override void Start()
		{
			m_environment.SendTo(m_player, ObjectVisibility.Public);
		}

		public override void Stop()
		{
		}

		public override bool Sees(IntPoint3 p)
		{
			return m_environment.Contains(p);
		}
	}
}
