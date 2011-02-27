using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	[GameObject(UseRef = true)]
	public class ItemObject : ServerGameObject, IItemObject
	{
		ItemObject()
			: base(ObjectType.Item)
		{
		}

		public ItemObject(ItemID itemID, MaterialID materialID)
			: base(ObjectType.Item)
		{
			Debug.Assert(itemID != Dwarrowdelf.ItemID.Undefined);
			Debug.Assert(materialID != Dwarrowdelf.MaterialID.Undefined);

			this.ItemID = itemID;
			this.SymbolID = this.ItemInfo.Symbol;
			this.MaterialID = materialID;
		}

		[GameProperty]
		public ItemID ItemID { get; private set; }
		public ItemInfo ItemInfo { get { return Dwarrowdelf.Items.GetItem(this.ItemID); } }
		public ItemClass ItemClass { get { return this.ItemInfo.ItemClass; } }

		public object ReservedBy { get; set; }

		static readonly PropertyDefinition NutritionalValueProperty =
			RegisterProperty(typeof(ItemObject), typeof(int), PropertyID.NutritionalValue, PropertyVisibility.Public, 0);
		static readonly PropertyDefinition RefreshmentValueProperty =
			RegisterProperty(typeof(ItemObject), typeof(int), PropertyID.RefreshmentValue, PropertyVisibility.Public, 0);

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
				ItemID = this.ItemID,
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
