using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public partial class World : IWorld
	{
		/* helpers for ironpython */
		public ItemObject[] IPItems
		{
			get { return m_objectMap.Values.OfType<ItemObject>().ToArray(); }
		}

		public Living[] IPLivings
		{
			get { return m_livings.List.ToArray(); }
		}

		public BaseGameObject IPGet(object target)
		{
			BaseGameObject ob = null;

			if (target is int)
			{
				ob = FindObject(new ObjectID((uint)(int)target));
			}

			if (ob == null)
				throw new Exception(String.Format("object {0} not found", target));

			return ob;
		}

		public ItemObject IPGetItem(object target)
		{
			ItemObject ob = null;

			if (target is int)
			{
				var oid = new ObjectID(ObjectType.Item, (uint)(int)target);

				ob = FindObject<ItemObject>(oid);
			}

			if (ob == null)
				throw new Exception(String.Format("object {0} not found", target));

			return ob;
		}

		public Environment IPGetEnv(object target)
		{
			Environment ob = null;

			if (target is int)
			{
				var oid = new ObjectID(ObjectType.Environment, (uint)(int)target);

				ob = FindObject<Environment>(oid);
			}

			if (ob == null)
				throw new Exception(String.Format("object {0} not found", target));

			return ob;
		}

		public Living IPGetLiving(object target)
		{
			Living ob = null;

			if (target is int)
			{
				var oid = new ObjectID(ObjectType.Living, (uint)(int)target);

				ob = FindObject<Living>(oid);
			}

			if (ob == null)
				throw new Exception(String.Format("object {0} not found", target));

			return ob;
		}
	}
}
