using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	[SaveGameObject]
	public sealed class ConstructJob : JobGroup
	{
		[SaveGameProperty]
		ConstructMode m_mode;
		[SaveGameProperty]
		IItemObject[] m_items;
		[SaveGameProperty]
		IEnvironmentObject m_environment;
		[SaveGameProperty]
		IntPoint3 m_location;

		[SaveGameProperty]
		int m_state;

		public ConstructJob(IJobObserver parent, ConstructMode mode, IItemObject[] items, IEnvironmentObject environment, IntPoint3 location)
			: base(parent)
		{
			m_mode = mode;
			m_items = items;
			m_environment = environment;
			m_location = location;

			m_state = 0;

			DirectionSet positioning;

			switch (mode)
			{
				case ConstructMode.Floor:
					positioning = DirectionSet.Planar;
					break;

				case ConstructMode.Pavement:
					positioning = DirectionSet.Exact;
					break;

				case ConstructMode.Wall:
					positioning = DirectionSet.Planar;
					break;

				default:
					throw new Exception();
			}

			AddSubJob(new FetchItems(this, m_environment, m_location, items, positioning));
		}

		ConstructJob(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);

			if (m_state == 0)
			{
				m_state = 1;
				AddSubJob(new AssignmentGroups.MoveConstructAssignment(this, m_mode, m_items, m_environment, m_location));
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
			return "ConstructJob";
		}
	}
}
