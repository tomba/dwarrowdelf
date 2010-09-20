using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.ActionJobs;

namespace Dwarrowdelf.Jobs.SerialActionJobs
{
	public class RunInCirclesJob : StaticSerialActionJob
	{
		readonly IEnvironment m_environment;

		public RunInCirclesJob(IJob parent, ActionPriority priority, IEnvironment environment)
			: base(parent, priority)
		{
			m_environment = environment;

			var jobs = new IActionJob[] {
				new MoveActionJob(this, priority, environment, new IntPoint3D(2, 18, 9), false),
				new MoveActionJob(this, priority, environment, new IntPoint3D(14, 18, 9), false),
				new MoveActionJob(this, priority, environment, new IntPoint3D(14, 28, 9), false),
				new MoveActionJob(this, priority, environment, new IntPoint3D(2, 28, 9), false),
				new MoveActionJob(this, priority, environment, new IntPoint3D(2, 18, 9), false),
			};

			SetSubJobs(jobs);
		}

		public override string ToString()
		{
			return "RunInCirclesJob";
		}
	}
}
