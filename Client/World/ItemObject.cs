using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	[SaveGameObject(ClientObject = true)]
	public sealed class ItemObject : ConcreteObject, IItemObject, ISaveGameDelegate
	{
		[Serializable]
		sealed class ItemObjectClientData
		{
			public BuildItemManager BuildItemManager;
		}

		/// <summary>
		/// For Design-time only
		/// </summary>
		public ItemObject()
			: base(null, new ObjectID(ObjectType.Item, 123456))
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

			ReceiveObjectData(data);
			ReceiveObjectDataEnd();
		}

		public ItemObject(World world, ObjectID objectID)
			: base(world, objectID)
		{

		}

		public override void ReceiveObjectData(BaseGameObjectData _data)
		{
			var data = (ItemData)_data;

			this.ItemInfo = Dwarrowdelf.Items.GetItemInfo(data.ItemID);

			base.ReceiveObjectData(_data);

			CreateItemDescription();

			SetSymbol();
		}

		object ISaveGameDelegate.GetSaveData()
		{
			ItemObjectClientData data = null;

			if (this.ItemCategory == Dwarrowdelf.ItemCategory.Workbench)
			{
				var buildItemManager = BuildItemManager.FindBuildItemManager(this);
				if (buildItemManager != null)
				{
					if (data == null)
						data = new ItemObjectClientData();

					data.BuildItemManager = buildItemManager;
				}
			}

			return data;
		}

		void ISaveGameDelegate.RestoreSaveData(object _data)
		{
			var data = (ItemObjectClientData)_data;

			if (data.BuildItemManager != null)
				BuildItemManager.AddBuildItemManager(data.BuildItemManager);
		}

		void CreateItemDescription()
		{
			var matInfo = this.Material;

			switch (this.ItemID)
			{
				case ItemID.UncutGem:
					this.Description = "uncut " + matInfo.Name;
					break;

				case ItemID.Ore:
					if (matInfo.ID == MaterialID.NativeGold)
						this.Description = matInfo.Adjective + " nugget";
					else
						this.Description = matInfo.Adjective + " ore";
					break;

				case ItemID.Gem:
					this.Description = matInfo.Name;
					break;

				case ItemID.Corpse:
					this.Description = String.Format("corpse of {0}", this.Name);
					break;

				default:
					if (this.Name != null)
						this.Description = matInfo.Adjective + " " + this.Name;
					else
						this.Description = matInfo.Adjective + " " + this.ItemInfo.Name;

					if (this.ItemInfo.IsInstallable && this.IsInstalled == false)
						this.Description = "uninstalled " + this.Description;

					break;
			}
		}

		public static event Action<ItemObject> IsReservedChanged;

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
				Notify("IsReserved");

				if (ItemObject.IsReservedChanged != null)
					ItemObject.IsReservedChanged(this);
			}
		}

		public bool IsReserved { get { return m_reservedBy != null; } }


		public static event Action<ItemObject> IsStockpiledChanged;

		[SaveGameProperty]
		Stockpile m_stockpiledBy;
		public Stockpile StockpiledBy
		{
			get { return m_stockpiledBy; }

			set
			{
				Debug.Assert(value == null || m_stockpiledBy == null);
				m_stockpiledBy = value;
				Notify("StockpiledBy");
				Notify("IsStockpiled");

				if (ItemObject.IsStockpiledChanged != null)
					ItemObject.IsStockpiledChanged(this);
			}
		}

		public bool IsStockpiled { get { return m_stockpiledBy != null; } }


		public ItemInfo ItemInfo { get; private set; }
		public ItemCategory ItemCategory { get { return this.ItemInfo.Category; } }
		public ItemID ItemID { get { return this.ItemInfo.ID; } }

		bool UseAltSymbol()
		{
			if (this.ItemID == Dwarrowdelf.ItemID.Door)
				return this.IsClosed;

			return false;
		}

		void SetSymbol()
		{
			this.SymbolID = ItemSymbols.GetSymbol(this.ItemID, UseAltSymbol());
		}

		public ArmorInfo ArmorInfo { get { return this.ItemInfo.ArmorInfo; } }
		public WeaponInfo WeaponInfo { get { return this.ItemInfo.WeaponInfo; } }

		public bool IsArmor { get { return this.ItemInfo.ArmorInfo != null; } }
		public bool IsWeapon { get { return this.ItemInfo.WeaponInfo != null; } }

		bool m_isEquipped;
		public bool IsEquipped
		{
			get { return m_isEquipped; }

			set
			{
				if (m_isEquipped == value)
					return;

				m_isEquipped = value;

				var p = this.Parent as LivingObject;
				if (p != null)
					p.OnItemIsEquippedChanged(this, value);

				Notify("Equipper");
				Notify("IsEquipped");
			}
		}


		ILivingObject IItemObject.Equipper { get { return this.Equipper; } }

		public LivingObject Equipper
		{
			get
			{
				if (this.IsEquipped)
					return (LivingObject)this.Parent;
				else
					return null;
			}
		}

		int m_quality;
		public int Quality
		{
			get { return m_quality; }
			private set { m_quality = value; Notify("Quality"); }
		}

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

		public static event Action<ItemObject> IsInstalledChanged;

		bool m_isInstalled;
		public bool IsInstalled
		{
			get { return m_isInstalled; }

			private set
			{
				m_isInstalled = value;
				CreateItemDescription();
				Notify("IsInstalled");
				if (ItemObject.IsInstalledChanged != null)
					ItemObject.IsInstalledChanged(this);
			}
		}

		bool m_isClosed;
		public bool IsClosed
		{
			get { return m_isClosed; }
			private set { m_isClosed = value; Notify("IsClosed"); SetSymbol(); }
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
				case PropertyID.Quality:
					this.Quality = (int)value;
					break;

				case PropertyID.NutritionalValue:
					this.NutritionalValue = (int)value;
					break;

				case PropertyID.RefreshmentValue:
					this.RefreshmentValue = (int)value;
					break;

				case PropertyID.ReservedByStr:
					this.ServerReservedBy = (string)value;
					break;

				case PropertyID.IsInstalled:
					this.IsInstalled = (bool)value;
					break;

				case PropertyID.IsClosed:
					this.IsClosed = (bool)value;
					break;

				case PropertyID.IsEquipped:
					this.IsEquipped = (bool)value;
					break;

				default:
					base.SetProperty(propertyID, value);
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

	public static class ItemSymbols
	{
		// ItemID -> SymbolID
		static SymbolID[] s_symbols;
		static SymbolID[] s_altSymbols;

		static ItemSymbols()
		{
			var max = EnumHelpers.GetEnumMax<ItemID>();
			s_symbols = new SymbolID[max + 1];
			s_altSymbols = new SymbolID[max + 1];

			var set = new Action<ItemID, SymbolID>((lid, sid) => s_symbols[(int)lid] = sid);

			set(ItemID.Food, SymbolID.Consumable);
			set(ItemID.Drink, SymbolID.Consumable);

			set(ItemID.Ore, SymbolID.Rock);

			foreach (var ii in Items.GetItemInfos(ItemCategory.Weapon))
				set(ii.ID, SymbolID.Weapon);

			foreach (var ii in Items.GetItemInfos(ItemCategory.Armor))
				set(ii.ID, SymbolID.Armor);

			foreach (var ii in Items.GetItemInfos(ItemCategory.Workbench))
				set(ii.ID, SymbolID.Workbench);

			foreach (var ii in Items.GetItemInfos(ItemCategory.Tools))
				set(ii.ID, SymbolID.Weapon);

			var symbolIDs = EnumHelpers.GetEnumValues<SymbolID>();

			for (int i = 1; i < s_symbols.Length; ++i)
			{
				if (s_symbols[i] != SymbolID.Undefined)
					continue;

				var itemID = (ItemID)i;

				var symbolID = symbolIDs.SingleOrDefault(sid => sid.ToString() == itemID.ToString());
				s_symbols[i] = symbolID;
			}

			// alternate symbols
			set = new Action<ItemID, SymbolID>((lid, sid) => s_altSymbols[(int)lid] = sid);

			set(ItemID.Door, SymbolID.DoorClosed);
		}

		public static SymbolID GetSymbol(ItemID itemID, bool useAlt)
		{
			SymbolID[] symbols = useAlt ? s_altSymbols : s_symbols;

			var sym = symbols[(int)itemID];

			if (sym == SymbolID.Undefined)
				return SymbolID.Unknown;

			return sym;
		}
	}
}
