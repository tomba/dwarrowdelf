using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.ActionJobs;

namespace Dwarrowdelf.Jobs.SerialActionJobs
{
	public class LoiterJob : DynamicSerialActionJob
	{
		readonly IEnvironment m_environment;

		public LoiterJob(IJob parent, ActionPriority priority, IEnvironment environment)
			: base(parent, priority)
		{
			m_environment = environment;
		}

		protected override IEnumerator<IActionJob> GetJobEnumerator()
		{
			while (true)
			{
				yield return new MoveActionJob(this, this.Priority, m_environment, new IntPoint3D(2, 18, 9), false);
				yield return new MoveActionJob(this, this.Priority, m_environment, new IntPoint3D(14, 18, 9), false);
				yield return new MoveActionJob(this, this.Priority, m_environment, new IntPoint3D(14, 28, 9), false);
				yield return new MoveActionJob(this, this.Priority, m_environment, new IntPoint3D(2, 28, 9), false);
				yield return new MoveActionJob(this, this.Priority, m_environment, new IntPoint3D(2, 18, 9), false);
			}
		}

		protected override Progress CheckProgress()
		{
			return Jobs.Progress.Ok;
		}

		public override string ToString()
		{
			return "LoiterJob";
		}
	}
}
