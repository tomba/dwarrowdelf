using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Server
{
	class KeyedObjectCollection : KeyedCollection<ObjectID, MovableObject>
	{
		public KeyedObjectCollection() : base(null, 10) { }

		protected override ObjectID GetKeyForItem(MovableObject item)
		{
			return item.ObjectID;
		}
	}
}
