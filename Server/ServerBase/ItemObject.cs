using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public class ItemObject : ServerGameObject, IItemObject
	{
		public ItemObject(ItemClass itemClass)
		{
			this.ItemClass = itemClass;
		}

		public ItemClass ItemClass { get; private set; }

		static readonly PropertyDefinition NutritionalValueProperty =
			RegisterProperty(typeof(ItemObject), PropertyID.NutritionalValue, PropertyVisibility.Public, 0);
		static readonly PropertyDefinition RefreshmentValueProperty =
			RegisterProperty(typeof(ItemObject), PropertyID.RefreshmentValue, PropertyVisibility.Public, 0);

		public int NutritionalValue
		{
			get { return (int)GetValue(NutritionalValueProperty); }
			set { SetValue(NutritionalValueProperty, value); }
		}

		public int RefreshmentValue
		{
			get { return (int)GetValue(RefreshmentValueProperty); }
			set { SetValue(RefreshmentValueProperty, value); }
		}

		public override BaseGameObjectData Serialize()
		{
			var data = new ItemData()
			{
				ObjectID = this.ObjectID,
				Environment = this.Parent != null ? this.Parent.ObjectID : ObjectID.NullObjectID,
				Location = this.Location,
				ItemClass = this.ItemClass,
				Properties = base.SerializeProperties(),
			};

			return data;
		}

		public override void SerializeTo(Action<Messages.ServerMessage> writer)
		{
			var msg = new Messages.ObjectDataMessage() { ObjectData = Serialize() };
			writer(msg);
		}

		public override string ToString()
		{
			return String.Format("ItemObject({0}/{1})", this.Name, this.ObjectID);
		}
	}
}
