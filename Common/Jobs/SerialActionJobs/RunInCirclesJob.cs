using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.ActionJobs;

namespace Dwarrowdelf.Jobs.SerialActionJobs
{
	public class RunInCirclesJob : SerialActionJob
	{
		IEnvironment m_environment;

		public RunInCirclesJob(IJob parent, ActionPriority priority, IEnvironment environment)
			: base(parent, priority)
		{
			m_environment = environment;

			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(2, 18, 9), false));
			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(14, 18, 9), false));
			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(14, 28, 9), false));
			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(2, 28, 9), false));
			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(2, 18, 9), false));
		}

		protected override void Cleanup()
		{
			m_environment = null;
		}

		public override string ToString()
		{
			return "RunInCirclesJob";
		}
	}
}
