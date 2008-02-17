using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	class ItemObject : ServerGameObject
	{
		public ItemObject(World world)
			: base(world)
		{
		}

		public string Name { get; set; }
	}
}
