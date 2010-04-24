using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame.Server
{
	public class ItemObject : ServerGameObject
	{
		public string Name { get; set; }
		public GameColor Color { get; set; }

		public ItemObject(World world)
			: base(world)
		{
		}

		public override ClientMsgs.Message Serialize()
		{
			var data = new ClientMsgs.ItemData();
			data.ObjectID = this.ObjectID;
			data.Name = this.Name;
			data.SymbolID = this.SymbolID;
			data.Environment = this.Parent != null ? this.Parent.ObjectID : ObjectID.NullObjectID;
			data.Location = this.Location;
			data.Color = this.Color;
			data.MaterialID = this.MaterialID;
			return data;
		}

		public override void SerializeTo(Action<ClientMsgs.Message> writer)
		{
			var msg = Serialize();
			writer(msg);
		}

		public override string ToString()
		{
			return String.Format("ItemObject({0}/{1})", this.Name, this.ObjectID);
		} 
	}
}
