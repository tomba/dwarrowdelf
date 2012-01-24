using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	[SaveGameObjectByRef]
	public sealed class MoveConstructAssignment : MoveBaseAssignment
	{
		[SaveGameProperty]
		ConstructMode m_mode;
		[SaveGameProperty]
		IItemObject[] m_items;
		[SaveGameProperty]
		IEnvironmentObject m_environment;
		[SaveGameProperty]
		IntPoint3 m_location;

		public MoveConstructAssignment(IJobObserver parent, ConstructMode mode, IItemObject[] items, IEnvironmentObject environment, IntPoint3 location)
			: base(parent, environment, items[0].Location)
		{
			m_mode = mode;
			m_items = items;
			m_environment = environment;
			m_location = location;
		}

		MoveConstructAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override DirectionSet GetPositioning()
		{
			return DirectionSet.Exact;
		}

		protected override IAssignment CreateAssignment()
		{
			return new ConstructAssignment(this, m_mode, m_environment, m_location, m_items);
		}

		public override string ToString()
		{
			return "MoveConstructAssignment";
		}
	}

}
