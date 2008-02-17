using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace MyGame
{
	class ItemCollection : ObservableCollection<ItemObject>
	{
	}

	class PlayerObject : ClientGameObject
	{
		public ItemCollection Inventory { get; private set; }
		
		public PlayerObject(ObjectID objectID)
			: base(objectID)
		{
			this.Inventory = new ItemCollection();
			ItemObject item1 = new ItemObject(new ObjectID(444));
			item1.Name = "gemi";
			item1.SymbolID = 3;

			Inventory.Add(item1);
			ItemObject item2 = new ItemObject(new ObjectID(446));
			item2.Name = "gemi2";
			item2.SymbolID = 5;
			Inventory.Add(item2);
		}

	}
}
