using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public sealed partial class World : IWorld
	{
		public BaseObject IPGet(object target)
		{
			ObjectID oid;

			if (target is int)
			{
				oid = new ObjectID((uint)(int)target);
			}
			else if (target is string)
			{
				if (ObjectID.TryParse((string)target, out oid) == false)
					return null;
			}
			else
			{
				return null;
			}

			var ob = FindObject(oid);

			if (ob == null)
				throw new Exception(String.Format("object {0} not found", target));

			return ob;
		}
	}
}
