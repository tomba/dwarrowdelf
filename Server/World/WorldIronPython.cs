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

		public IBaseGameObject IPGet(object target)
		{
			IBaseGameObject ob = null;

			if (target is int)
			{
				ob = FindObject(new ObjectID((int)target));
			}

			if (ob == null)
				throw new Exception(String.Format("object {0} not found", target));

			return ob;
		}
	}
}
