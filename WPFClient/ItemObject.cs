using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace MyGame
{
	class ItemObject : ClientGameObject
	{
		public ItemObject(ObjectID objectID)
			: base(objectID)
		{
			
		}

//		public string Name { get; set; }

		public override string ToString()
		{
			return String.Format("Item({0})", this.ObjectID);
		}
	}
}
