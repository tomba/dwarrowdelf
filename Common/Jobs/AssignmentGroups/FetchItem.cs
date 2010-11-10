using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public class FetchItem : StaticAssignmentGroup
	{
		public IItemObject Item { get; private set; }

		public FetchItem(IJob parent, ActionPriority priority, IEnvironment env, IntPoint3D location, IItemObject item)
			: base(parent, priority)
		{
			this.Item = item;

			var jobs = new IAssignment[] {
				new MoveAssignment(this, priority, item.Environment, item.Location, DirectionSet.Exact),
				new GetItemAssignment(this, priority, item),
				new MoveAssignment(this, priority, env, location, DirectionSet.Exact),
				new DropItemAssignment(this, priority, item),
			};

			SetAssignments(jobs);
		}

		public override string ToString()
		{
			return "FetchItem";
		}
	}
}
