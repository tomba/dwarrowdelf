using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public abstract class AreaObject : BaseObject, IAreaObject
	{
		[SaveGameProperty]
		public EnvironmentObject Environment { get; private set; }
		IEnvironmentObject IAreaObject.Environment { get { return this.Environment; } }

		[SaveGameProperty]
		public IntGrid2Z Area { get; private set; }

		protected AreaObject(ObjectType objectType, IntGrid2Z area)
			: base(objectType)
		{
			this.Area = area;
		}

		protected AreaObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
		}

		public bool Contains(IntPoint3 point)
		{
			return this.Area.Contains(point);
		}

		protected void SetEnvironment(EnvironmentObject env)
		{
			if (this.Environment != null)
				this.Environment.RemoveLargeObject(this);

			this.Environment = env;

			if (this.Environment != null)
				this.Environment.AddLargeObject(this);
		}
	}
}
