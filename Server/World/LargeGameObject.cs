using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public abstract class LargeGameObject : BaseGameObject, ILargeGameObject
	{
		[SaveGameProperty]
		public Environment Environment { get; private set; }
		IEnvironment ILargeGameObject.Environment { get { return this.Environment as IEnvironment; } }

		[SaveGameProperty]
		public IntRectZ Area { get; private set; }

		protected LargeGameObject(ObjectType objectType, IntRectZ area)
			: base(objectType)
		{
			this.Area = area;
		}

		protected LargeGameObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
		}

		public bool Contains(IntPoint3D point)
		{
			return this.Area.Contains(point);
		}

		protected void SetEnvironment(Environment env)
		{
			if (this.Environment != null)
				this.Environment.RemoveLargeObject(this);

			this.Environment = env;

			if (this.Environment != null)
				this.Environment.AddLargeObject(this);
		}
	}
}
