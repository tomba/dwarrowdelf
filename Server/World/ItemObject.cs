using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	// Inherited in Area projects. Could be sealed otherwise.
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
		public ItemInfo ItemInfo { get { return Dwarrowdelf.Items.GetItemInfo(this.ItemID); } }
		public ItemCategory ItemCategory { get { return this.ItemInfo.Category; } }

		public ArmorInfo ArmorInfo { get { return this.ItemInfo.ArmorInfo; } }
		public WeaponInfo WeaponInfo { get { return this.ItemInfo.WeaponInfo; } }

		public bool IsArmor { get { return this.ItemInfo.ArmorInfo != null; } }
		public bool IsWeapon { get { return this.ItemInfo.WeaponInfo != null; } }

		protected override bool OkToMove()
		{
			return !this.IsWorn && !this.IsWielded && !this.IsInstalled;
		}

		protected override void OnEnvironmentChanging()
		{
			// Ensure that the item is not installed when moved. This can happen when forcibly moved.
			if (this.IsInstalled)
				this.IsInstalled = false;

			base.OnEnvironmentChanging();
		}

		protected override void OnLocationChanging()
		{
			// Ensure that the item is not installed when moved. This can happen when forcibly moved.
			if (this.IsInstalled)
				this.IsInstalled = false;

			base.OnLocationChanging();
		}

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
			set { if (m_reservedByStr == value) return; m_reservedByStr = value; NotifyString(PropertyID.ReservedByStr, value); }
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

		[SaveGameProperty("IsInstalled")]
		bool m_isInstalled;
		public bool IsInstalled
		{
			get { return m_isInstalled; }
			set
			{
				if (m_isInstalled == value)
					return;

				Debug.Assert(this.Environment != null);

				m_isInstalled = value;
				NotifyBool(PropertyID.IsInstalled, value);

				CheckBlock();
			}
		}

		[SaveGameProperty("IsClosed")]
		bool m_isClosed;
		public bool IsClosed
		{
			get { return m_isClosed; }
			set { if (m_isClosed == value) return; m_isClosed = value; NotifyBool(PropertyID.IsClosed, value); CheckBlock(); }
		}

		[SaveGameProperty("IsBlocking")]
		bool m_isBlocking;

		void CheckBlock()
		{
			var block = this.IsInstalled && this.IsClosed;

			if (block != m_isBlocking)
			{
				this.Environment.SetTileFlags(this.Location, TileFlags.ItemBlocks, block);
				m_isBlocking = block;
			}
		}

		LivingObject m_wearer;
		public LivingObject Wearer
		{
			get
			{
				Debug.Assert(this.IsArmor);
				return m_wearer;
			}
			internal set
			{
				Debug.Assert(this.IsArmor);
				m_wearer = value;
			}
		}

		LivingObject m_wielder;
		public LivingObject Wielder
		{
			get
			{
				Debug.Assert(this.IsWeapon);
				return m_wielder;
			}
			internal set
			{
				Debug.Assert(this.IsWeapon);
				m_wielder = value;
			}
		}

		ILivingObject IItemObject.Wearer { get { return this.Wearer; } }
		ILivingObject IItemObject.Wielder { get { return this.Wielder; } }

		public bool IsWorn { get { return this.IsArmor && this.Wearer != null; } }
		public bool IsWielded { get { return this.IsWeapon && this.Wielder != null; } }

		protected override void CollectObjectData(BaseGameObjectData baseData, ObjectVisibility visibility)
		{
			base.CollectObjectData(baseData, visibility);

			var data = (ItemData)baseData;

			data.ItemID = this.ItemID;
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			var data = new ItemData();

			CollectObjectData(data, visibility);

			player.Send(new Messages.ObjectDataMessage(data));

			base.SendTo(player, visibility);
		}

		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();

			props[PropertyID.NutritionalValue] = m_nutritionalValue;
			props[PropertyID.RefreshmentValue] = m_refreshmentValue;
			props[PropertyID.ReservedByStr] = m_reservedByStr;
			props[PropertyID.IsInstalled] = m_isInstalled;
			props[PropertyID.IsClosed] = m_isClosed;

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

	public sealed class ItemObjectBuilder : ConcreteObjectBuilder
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
