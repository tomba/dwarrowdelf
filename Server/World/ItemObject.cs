using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	[SaveGameObjectByRef]
	public class ItemObject : ConcreteObject, IItemObject
	{
		internal static ItemObject Create(World world, ItemObjectBuilder builder)
		{
			var ob = new ItemObject(builder);
			ob.Initialize(world);
			return ob;
		}

		protected ItemObject(SaveGameContext ctx)
			: base(ctx, ObjectType.Item)
		{
		}

		protected ItemObject(ItemObjectBuilder builder)
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
		public ItemCategory ItemCategory { get { return this.ItemInfo.Category; } }

		[SaveGameProperty("ReservedBy")]
		object m_reservedBy;
		public object ReservedBy
		{
			get { return m_reservedBy; }
			set
			{
				Debug.Assert(value == null || m_reservedBy == null);
				m_reservedBy = value;
				if (value != null)
					this.ReservedByStr = value.ToString();
				else
					this.ReservedByStr = null;
			}
		}

		// String representation of ReservedBy, for client use
		[SaveGameProperty("ReservedByStr")]
		string m_reservedByStr;
		public string ReservedByStr
		{
			get { return m_reservedByStr; }
			set { if (m_reservedByStr == value) return; m_reservedByStr = value; NotifyObject(PropertyID.ReservedByStr, value); }
		}

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

		protected override void SerializeTo(BaseGameObjectData data, ObjectVisibility visibility)
		{
			base.SerializeTo(data, visibility);

			SerializeToInternal((ItemData)data, visibility);
		}

		void SerializeToInternal(ItemData data, ObjectVisibility visibility)
		{
			data.ItemID = this.ItemID;
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			var data = new ItemData();

			SerializeTo(data, visibility);

			player.Send(new Messages.ObjectDataMessage(data));

			base.SendTo(player, visibility);
		}

		protected override Dictionary<PropertyID, object> SerializeProperties(ObjectVisibility visibility)
		{
			var props = base.SerializeProperties(visibility);
			if (visibility == ObjectVisibility.All)
			{
				props[PropertyID.NutritionalValue] = m_nutritionalValue;
				props[PropertyID.RefreshmentValue] = m_refreshmentValue;
				props[PropertyID.ReservedByStr] = m_reservedByStr;
			}
			return props;
		}

		public override string ToString()
		{
			string name;

			if (this.IsDestructed)
				name = "<DestructedObject>";
			else if (this.Name != null)
				name = this.Name;
			else
				name = this.ItemInfo.Name;

			return String.Format("{0} ({1})", name, this.ObjectID);
		}
	}

	public class ItemObjectBuilder : LocatableGameObjectBuilder
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
			return ItemObject.Create(world, this);
		}
	}
}
