﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public class ItemObject : ServerGameObject, IItemObject
	{
		public ItemObject(ItemID itemID, MaterialID materialID)
			: base(ObjectType.Item)
		{
			Debug.Assert(itemID != Dwarrowdelf.ItemID.Undefined);
			Debug.Assert(materialID != Dwarrowdelf.MaterialID.Undefined);

			this.ItemInfo = Dwarrowdelf.Items.GetItem(itemID);
			this.SymbolID = this.ItemInfo.Symbol;
			this.MaterialID = materialID;
		}

		public ItemInfo ItemInfo { get; private set; }
		public ItemClass ItemClass { get { return this.ItemInfo.ItemClass; } }
		public ItemID ItemID { get { return this.ItemInfo.ItemID; } }

		public object ReservedBy { get; set; }

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
