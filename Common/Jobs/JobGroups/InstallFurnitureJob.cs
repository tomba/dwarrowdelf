using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	[SaveGameObjectByRef]
	public sealed class InstallFurnitureJob : JobGroup
	{
		[SaveGameProperty]
		IItemObject m_item;
		[SaveGameProperty]
		IntPoint3D m_location;

		[SaveGameProperty]
		int m_state;

		public InstallFurnitureJob(IJobObserver parent, IItemObject item, IEnvironmentObject env, IntPoint3D location)
			: base(parent)
		{
			m_item = item;
			m_location = location;

			m_state = 0;

			AddSubJob(new AssignmentGroups.HaulItemAssignment(this, env, location, item));
		}

		InstallFurnitureJob(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnStatusChanged(JobStatus status)
		{
			base.OnStatusChanged(status);
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);

			if (m_state == 0)
			{
				m_state = 1;
				AddSubJob(new AssignmentGroups.MoveInstallFurnitureAssignment(this, m_item));
			}
			else if (m_state == 1)
			{
				SetStatus(JobStatus.Done);
			}
			else
			{
				throw new Exception();
			}
		}

		public override string ToString()
		{
			return "InstallFurnitureJob";
		}
	}
}
