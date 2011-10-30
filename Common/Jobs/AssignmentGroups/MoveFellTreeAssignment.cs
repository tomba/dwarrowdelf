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
	public class MoveFellTreeAssignment : MoveBaseAssignment
	{
		public MoveFellTreeAssignment(IJobObserver parent, IEnvironment environment, IntPoint3D location)
			: base(parent, environment, location)
		{
		}

		protected MoveFellTreeAssignment(SaveGameContext ctx)
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
