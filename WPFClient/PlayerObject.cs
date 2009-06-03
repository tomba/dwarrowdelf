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
			this.SymbolID = 3;
		}

	}
}
