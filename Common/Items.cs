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
		Food,
		Drink,
		Rock,
		Ore,
		Gem,
		UncutGem,
		Corpse,

		Chair,
		Table,
		Door,
		Bed,
		Barrel,
		Bucket,

		Contraption,

		/// <summary>
		/// Used for dynamically initialized items
		/// </summary>
		Custom,
	}

	public enum ItemCategory
	{
		Undefined = 0,
		Furniture,
		Food,
		Drink,
		Gem,
		RawMaterial,
		Corpse,
		Other,

		Custom,
	}

	public class ItemInfo
	{
		public ItemID ID { get; set; }
		public string Name { get; set; }
		public ItemCategory Category { get; set; }
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

			var max = items.Max(i => (int)i.ID);
			s_items = new ItemInfo[max + 1];

			foreach (var item in items)
			{
				if (s_items[(int)item.ID] != null)
					throw new Exception();

				if (item.Name == null)
					item.Name = item.ID.ToString().ToLowerInvariant();

				s_items[(int)item.ID] = item;
			}

			s_items[(int)ItemID.Custom] = new ItemInfo()
			{
				ID = ItemID.Custom,
				Name = "<undefined>",
				Category = ItemCategory.Custom,
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
