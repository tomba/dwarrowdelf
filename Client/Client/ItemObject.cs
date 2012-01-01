using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	[SaveGameObjectByRef(ClientObject = true)]
	sealed class ItemObject : ConcreteObject, IItemObject
	{
		/// <summary>
		/// For Design-time only
		/// </summary>
		public ItemObject()
			: base(null, ObjectID.NullObjectID)
		{
			var r = new Random();

			var props = new Tuple<PropertyID, object>[]
			{
				new Tuple<PropertyID, object>(PropertyID.MaterialID, MaterialID.Bronze),
			};

			var data = new ItemData()
			{
				ObjectID = new ObjectID(ObjectType.Item, (uint)r.Next(5000)),
				ItemID = Dwarrowdelf.ItemID.ChainMail,
				CreationTick = 123,
				CreationTime = DateTime.Now,
				Properties = props,
			};

			Deserialize(data);
		}

		public ItemObject(World world, ObjectID objectID)
			: base(world, objectID)
		{

		}

		[SaveGameProperty]
		object m_reservedBy;
		public object ReservedBy
		{
			get { return m_reservedBy; }
			set
			{
				Debug.Assert(value == null || m_reservedBy == null);
				m_reservedBy = value;
				Notify("ReservedBy");
			}
		}

		public ItemInfo ItemInfo { get; private set; }
		public ItemCategory ItemCategory { get { return this.ItemInfo.Category; } }
		public ItemID ItemID { get { return this.ItemInfo.ID; } }

		public ArmorInfo ArmorInfo { get { return this.ItemInfo.ArmorInfo; } }
		public WeaponInfo WeaponInfo { get { return this.ItemInfo.WeaponInfo; } }

		public bool IsArmor { get { return this.ItemInfo.ArmorInfo != null; } }
		public bool IsWeapon { get { return this.ItemInfo.WeaponInfo != null; } }

		public LivingObject Wearer { get; internal set; }
		public LivingObject Wielder { get; internal set; }

		ILivingObject IItemObject.Wearer { get { return this.Wearer; } }
		ILivingObject IItemObject.Wielder { get { return this.Wielder; } }

		public bool IsWorn { get { return this.Wearer != null; } }
		public bool IsWielded { get { return this.Wielder != null; } }

		int m_nutritionalValue;
		public int NutritionalValue
		{
			get { return m_nutritionalValue; }
			private set { m_nutritionalValue = value; Notify("NutritionalValue"); }
		}

		int m_refreshmentValue;
		public int RefreshmentValue
		{
			get { return m_refreshmentValue; }
			private set { m_refreshmentValue = value; Notify("RefreshmentValue"); }
		}

		string m_serverReservedBy;
		public string ServerReservedBy
		{
			get { return m_serverReservedBy; }
			private set { m_serverReservedBy = value; Notify("ServerReservedBy"); }
		}

		public override void SetProperty(PropertyID propertyID, object value)
		{
			switch (propertyID)
			{
				case PropertyID.NutritionalValue:
					this.NutritionalValue = (int)value;
					break;

				case PropertyID.RefreshmentValue:
					this.RefreshmentValue = (int)value;
					break;

				case PropertyID.ReservedByStr:
					this.ServerReservedBy = (string)value;
					break;

				default:
					base.SetProperty(propertyID, value);
					break;
			}
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (ItemData)_data;

			this.ItemInfo = Dwarrowdelf.Items.GetItemInfo(data.ItemID);
			this.SymbolID = ItemSymbols.GetSymbol(this.ItemID);

			base.Deserialize(_data);

			var matInfo = this.Material;
			switch (this.ItemID)
			{
				case ItemID.UncutGem:
					this.Description = "uncut " + matInfo.Name;
					break;

				case ItemID.Rock:
				case ItemID.Ore:
					if (matInfo.ID == MaterialID.NativeGold)
					{
						this.Description = matInfo.Adjective + " nugget";
					}
					else
					{
						this.Description = matInfo.Adjective + " rock";
					}
					break;

				case ItemID.Gem:
					this.Description = matInfo.Name;
					break;

				case ItemID.Corpse:
					this.Description = String.Format("Corpse of {0}", this.Name);
					break;

				default:
					if (this.Name != null)
						this.Description = matInfo.Adjective + " " + this.Name;
					else
						this.Description = matInfo.Adjective + " " + this.ItemInfo.Name;
					break;
			}
		}

		public override string ToString()
		{
			string name;

			if (this.IsDestructed)
				name = "<DestructedObject>";
			else if (!this.IsInitialized)
				name = "<UninitializedObject>";
			else if (this.Name != null)
				name = this.Name;
			else
				name = this.ItemInfo.Name;

			return String.Format("{0} ({1})", name, this.ObjectID);
		}
	}

	static class ItemSymbols
	{
		// ItemID -> SymbolID
		static SymbolID[] s_symbols;

		static ItemSymbols()
		{
			var arr = (ItemID[])Enum.GetValues(typeof(ItemID));
			var max = arr.Max(i => (int)i);
			s_symbols = new SymbolID[max + 1];

			var set = new Action<ItemID, SymbolID>((lid, sid) => s_symbols[(int)lid] = sid);

			set(ItemID.Food, SymbolID.Consumable);
			set(ItemID.Drink, SymbolID.Consumable);
			set(ItemID.Corpse, SymbolID.Rock);

			set(ItemID.Ore, SymbolID.Rock);

			foreach (var ii in Items.GetItemInfos(ItemCategory.Weapon))
				set(ii.ID, SymbolID.Weapon);

			foreach (var ii in Items.GetItemInfos(ItemCategory.Armor))
				set(ii.ID, SymbolID.Armor);

			var symbolIDs = (SymbolID[])Enum.GetValues(typeof(SymbolID));

			for (int i = 1; i < s_symbols.Length; ++i)
			{
				if (s_symbols[i] != SymbolID.Undefined)
					continue;

				var itemID = (ItemID)i;

				var symbolID = symbolIDs.Single(sid => sid.ToString() == itemID.ToString());
				s_symbols[i] = symbolID;
			}
		}

		public static SymbolID GetSymbol(ItemID id)
		{
			var sym = s_symbols[(int)id];

			if (sym == SymbolID.Undefined)
				return SymbolID.Unknown;

			return sym;
		}
	}
}
