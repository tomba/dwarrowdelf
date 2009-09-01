using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class ItemObject : ServerGameObject
	{
		public ItemObject(World world)
			: base(world)
		{
		}

		public ClientMsgs.ItemData Serialize()
		{
			var data = new ClientMsgs.ItemData();
			data.ObjectID = this.ObjectID;
			data.Name = this.Name;
			data.SymbolID = this.SymbolID;
			data.Environment = this.Environment != null ? this.Environment.ObjectID : ObjectID.NullObjectID;
			data.Location = this.Location;
			data.Color = this.Color;
			return data;
		}
	}
}
