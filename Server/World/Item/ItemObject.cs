using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	// Inherited in Area projects. Could be sealed otherwise.
	[SaveGameObject]
	public partial class ItemObject : ConcreteObject, IItemObject
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
			m_quality = builder.Quality;
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
			return !this.IsEquipped && !this.IsInstalled;
		}

		protected override void OnParentChanging()
		{
			// Ensure that the item is not installed when moved. This can happen when forcibly moved.
			if (this.IsInstalled)
				this.IsInstalled = false;

			// Ensure that the item is not equipped when moved. This can happen when forcibly moved.
			if (this.IsEquipped)
				this.IsEquipped = false;

			base.OnParentChanging();
		}

		protected override void OnLocationChanging()
		{
			// Ensure that the item is not installed when moved. This can happen when forcibly moved.
			if (this.IsInstalled)
				this.IsInstalled = false;

			base.OnLocationChanging();
		}

		[SaveGameProperty("IsBlocking")]
		bool m_isBlocking;

		public bool IsBlocking { get { return m_isBlocking; } }

		void CheckBlock()
		{
			var block = this.IsInstalled && this.IsClosed;

			if (block != m_isBlocking)
			{
				m_isBlocking = block;
				this.Environment.ItemBlockChanged(this.Location);
			}
		}

		protected override void CollectObjectData(BaseGameObjectData baseData, ObjectVisibility visibility)
		{
			base.CollectObjectData(baseData, visibility);

			var data = (ItemData)baseData;

			data.ItemID = this.ItemID;
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			Debug.Assert(visibility != ObjectVisibility.None);

			var data = new ItemData();

			CollectObjectData(data, visibility);

			player.Send(new Messages.ObjectDataMessage(data));

			base.SendTo(player, visibility);

			player.Send(new Messages.ObjectDataEndMessage() { ObjectID = this.ObjectID });
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
		public int Quality { get; set; }
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
