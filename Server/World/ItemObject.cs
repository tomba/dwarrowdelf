using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	[SaveGameObject(UseRef = true)]
	public class ItemObject : ServerGameObject, IItemObject
	{
		protected ItemObject(SaveGameContext ctx)
			: base(ctx, ObjectType.Item)
		{
		}

		internal protected ItemObject(ItemObjectBuilder builder)
			: base(ObjectType.Item, builder)
		{
			Debug.Assert(builder.ItemID != Dwarrowdelf.ItemID.Undefined);
			Debug.Assert(builder.MaterialID != Dwarrowdelf.MaterialID.Undefined);

			this.ItemID = builder.ItemID;
			m_nutritionalValue = builder.NutritionalValue;
			m_refreshmentValue = builder.RefreshmentValue;
		}

		[SaveGameProperty]
		public ItemID ItemID { get; private set; }
		public ItemInfo ItemInfo { get { return Dwarrowdelf.Items.GetItem(this.ItemID); } }
		public ItemClass ItemClass { get { return this.ItemInfo.ItemClass; } }

		public object ReservedBy { get; set; }

		[SaveGameProperty("NutritionalValue")]
		int m_nutritionalValue;
		public int NutritionalValue
		{
			get { return m_nutritionalValue; }
			set { if (m_nutritionalValue == value) return; m_nutritionalValue = value; NotifyInt(PropertyID.NutritionalValue, value); }
		}

		[SaveGameProperty("RefreshmentValue")]
		int m_refreshmentValue;
		public int RefreshmentValue
		{
			get { return m_refreshmentValue; }
			set { if (m_refreshmentValue == value) return; m_refreshmentValue = value; NotifyInt(PropertyID.RefreshmentValue, value); }
		}

		public override BaseGameObjectData Serialize()
		{
			var data = new ItemData()
			{
				ObjectID = this.ObjectID,
				Environment = this.Parent != null ? this.Parent.ObjectID : ObjectID.NullObjectID,
				Location = this.Location,
				ItemID = this.ItemID,
				Properties = SerializeProperties().Select(kvp => new Tuple<PropertyID, object>(kvp.Key, kvp.Value)).ToArray(),
			};

			return data;
		}

		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();
			props[PropertyID.NutritionalValue] = m_nutritionalValue;
			props[PropertyID.RefreshmentValue] = m_refreshmentValue;
			return props;
		}

		public override string ToString()
		{
			return String.Format("ItemObject({0}/{1})", this.Name, this.ObjectID);
		}
	}

	public class ItemObjectBuilder : ServerGameObjectBuilder
	{
		public ItemID ItemID { get; set; }
		public int NutritionalValue { get; set; }
		public int RefreshmentValue { get; set; }

		public ItemObjectBuilder(ItemID itemID, MaterialID materialID)
		{
			this.ItemID = itemID;
			this.MaterialID = materialID;
		}

		public ItemObject Create(World world)
		{
			if (this.SymbolID == SymbolID.Undefined)
				this.SymbolID = Dwarrowdelf.Items.GetItem(this.ItemID).Symbol;

			var item = new ItemObject(this);
			item.Initialize(world);
			return item;
		}
	}
}
