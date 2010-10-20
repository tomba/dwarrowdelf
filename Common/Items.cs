using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;

namespace Dwarrowdelf
{
	public enum ItemType : byte
	{
		Undefined = 0,
		Log,
		Chair,
		Table,
		Food,
		Drink,
		Diamond,
		Rock,

		Custom,
	}

	public enum ItemClass : byte
	{
		Undefined = 0,
		Furniture,
		Food,
		Drink,
		Gem,
		Material,

		Custom,
	}

	public class ItemInfo
	{
		public ItemInfo(ItemType itemID, ItemClass itemClass, SymbolID symbol)
		{
			this.ItemType = itemID;
			this.Name = itemID.ToString();
			this.ItemClass = itemClass;
			this.Symbol = symbol;
		}

		public ItemType ItemType { get; private set; }
		public string Name { get; private set; }
		public ItemClass ItemClass { get; private set; }
		public SymbolID Symbol { get; private set; }
	}

	public static class Items
	{
		static ItemInfo[] s_itemList;

		static Items()
		{
			var arr = (ItemType[])Enum.GetValues(typeof(ItemType));
			var max = arr.Max();
			s_itemList = new ItemInfo[(int)max + 1];

			foreach (var field in typeof(Items).GetFields())
			{
				if (field.FieldType != typeof(ItemInfo))
					continue;

				var info = (ItemInfo)field.GetValue(null);
				s_itemList[(int)info.ItemType] = info;
			}
		}

		public static ItemInfo GetItem(ItemType id)
		{
			return s_itemList[(int)id];
		}

		public static readonly ItemInfo Undefined = new ItemInfo(ItemType.Undefined, ItemClass.Undefined, SymbolID.Undefined);

		public static readonly ItemInfo Log = new ItemInfo(ItemType.Log, ItemClass.Material, SymbolID.Log);
		public static readonly ItemInfo Chair = new ItemInfo(ItemType.Chair, ItemClass.Furniture, SymbolID.Key);
		public static readonly ItemInfo Table = new ItemInfo(ItemType.Table, ItemClass.Furniture, SymbolID.Key);
		public static readonly ItemInfo Food = new ItemInfo(ItemType.Food, ItemClass.Food, SymbolID.Consumable);
		public static readonly ItemInfo Drink = new ItemInfo(ItemType.Drink, ItemClass.Drink, SymbolID.Consumable);
		public static readonly ItemInfo Diamond = new ItemInfo(ItemType.Diamond, ItemClass.Gem, SymbolID.Gem);
		public static readonly ItemInfo Rock = new ItemInfo(ItemType.Rock, ItemClass.Material, SymbolID.Rock);

		public static readonly ItemInfo Custom = new ItemInfo(ItemType.Custom, ItemClass.Custom, SymbolID.Undefined);
	}
}
