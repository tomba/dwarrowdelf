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

		public LivingObject[] IPLivings
		{
			get { return m_livings.List.ToArray(); }
		}

		public BaseObject IPGet(object target)
		{
			BaseObject ob = null;

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

		public EnvironmentObject IPGetEnv(object target)
		{
			EnvironmentObject ob = null;

			if (target is int)
			{
				var oid = new ObjectID(ObjectType.Environment, (uint)(int)target);

				ob = FindObject<EnvironmentObject>(oid);
			}

			if (ob == null)
				throw new Exception(String.Format("object {0} not found", target));

			return ob;
		}

		public LivingObject IPGetLiving(object target)
		{
			LivingObject ob = null;

			if (target is int)
			{
				var oid = new ObjectID(ObjectType.Living, (uint)(int)target);

				ob = FindObject<LivingObject>(oid);
			}

			if (ob == null)
				throw new Exception(String.Format("object {0} not found", target));

			return ob;
		}
	}
}
