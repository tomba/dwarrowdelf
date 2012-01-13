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
	public sealed class MoveFellTreeAssignment : MoveBaseAssignment
	{
		public MoveFellTreeAssignment(IJobObserver parent, IEnvironmentObject environment, IntPoint3 location)
			: base(parent, environment, location)
		{
		}

		MoveFellTreeAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override DirectionSet GetPositioning()
		{
			return DirectionSet.Planar;
		}

		protected override IAssignment CreateAssignment()
		{
			return new FellTreeAssignment(this, this.Environment, this.Location);
		}

		public override string ToString()
		{
			return "MoveFellTreeAssignment";
		}
	}
}
