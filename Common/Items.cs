using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public enum ItemID
	{
		Undefined = 0,
		Log,
		Chair,
		Table,
		Food,
		Drink,
		Rock,
		Ore,
		Gem,
		UncutGem,
		Corpse,

		/// <summary>
		/// Used for dynamically initialized items
		/// </summary>
		Custom,
	}

	public enum ItemClass
	{
		Undefined = 0,
		Furniture,
		Food,
		Drink,
		Gem,
		RawMaterial,
		Corpse,

		Custom,
	}

	public class ItemInfo
	{
		public ItemID ItemID { get; set; }
		public string Name { get; set; }
		public ItemClass ItemClass { get; set; }
		public SymbolID Symbol { get; set; }
	}

	public static class Items
	{
		static ItemInfo[] s_items;

		static Items()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			ItemInfo[] items;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Data.Items.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = asm,
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					items = (ItemInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = items.Max(i => (int)i.ItemID);
			s_items = new ItemInfo[max + 1];

			foreach (var item in items)
			{
				if (s_items[(int)item.ItemID] != null)
					throw new Exception();

				if (item.Name == null)
					item.Name = item.ItemID.ToString().ToLowerInvariant();

				s_items[(int)item.ItemID] = item;
			}

			s_items[(int)ItemID.Custom] = new ItemInfo()
			{
				ItemID = ItemID.Custom,
				Name = "<undefined>",
				ItemClass = ItemClass.Custom,
				Symbol = SymbolID.Undefined,
			};
		}

		public static ItemInfo GetItem(ItemID id)
		{
			Debug.Assert(id != ItemID.Undefined);
			Debug.Assert(s_items[(int)id] != null);

			return s_items[(int)id];
		}
	}
}
