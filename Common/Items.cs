using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Diagnostics;

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

	public class ItemInfo
	{
		public ItemType ItemType { get; set; }
		public string Name { get; set; }
		public ItemClass ItemClass { get; set; }
		public SymbolID Symbol { get; set; }
	}

	public static class Items
	{
		static ItemInfo[] s_items;

		static Items()
		{
			ItemInfo[] items;

			using (var stream = System.IO.File.OpenRead("Items.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = System.Reflection.Assembly.GetCallingAssembly(),
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					items = (ItemInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = items.Max(i => (int)i.ItemType);
			s_items = new ItemInfo[max + 1];

			foreach (var item in items)
			{
				if (s_items[(int)item.ItemType] != null)
					throw new Exception();

				if (item.Name == null)
					item.Name = item.ItemType.ToString();

				s_items[(int)item.ItemType] = item;
			}

			s_items[(int)ItemType.Custom] = new ItemInfo()
			{
				ItemType = ItemType.Custom,
				Name = "<undefined>",
				ItemClass = ItemClass.Custom,
				Symbol = SymbolID.Undefined,
			};
		}

		public static ItemInfo GetItem(ItemType id)
		{
			Debug.Assert(id != ItemType.Undefined);
			Debug.Assert(s_items[(int)id] != null);

			return s_items[(int)id];
		}
	}
}
