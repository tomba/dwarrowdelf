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

		/// <summary>
		/// Used for dynamically initialized items
		/// </summary>
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

	public class ItemCollection : Dictionary<ItemType, ItemInfo>
	{
	}

	[DictionaryKeyProperty("ItemType")]
	public class ItemInfo
	{
		string m_name;

		public ItemType ItemType { get; set; }
		public string Name
		{
			// XXX
			get { if (m_name == null) m_name = this.ItemType.ToString(); return m_name; }
			set { m_name = value; }
		}
		public ItemClass ItemClass { get; set; }
		public SymbolID Symbol { get; set; }
	}

	public static class Items
	{
		static ItemInfo[] s_itemList;

		static Items()
		{
			ItemCollection items;

			using (var stream = System.IO.File.OpenRead("Items.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = System.Reflection.Assembly.GetCallingAssembly(),
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					items = (ItemCollection)System.Xaml.XamlServices.Load(reader);
			}

			var max = (int)items.Keys.Max();
			s_itemList = new ItemInfo[(int)max + 1];

			foreach (var item in items)
				s_itemList[(int)item.Key] = item.Value;

			s_itemList[0] = new ItemInfo()
			{
				ItemType = ItemType.Undefined,
				Name = "<undefined>",
				ItemClass = ItemClass.Undefined,
				Symbol = SymbolID.Undefined,
			};

			s_itemList[(int)ItemType.Custom] = new ItemInfo()
			{
				ItemType = ItemType.Custom,
				Name = "<undefined>",
				ItemClass = ItemClass.Custom,
				Symbol = SymbolID.Undefined,
			};
		}

		public static ItemInfo GetItem(ItemType id)
		{
			return s_itemList[(int)id];
		}
	}
}
