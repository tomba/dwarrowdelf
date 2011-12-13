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
		Block,
		Corpse,

		Chair,
		Table,
		Door,
		Bed,
		Barrel,
		Bucket,

		ShortSword,
		BattleAxe,

		ChainMail,
		PlateMail,

		Contraption,
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
		Weapon,
		Armor,
		Other,
	}

	public class ItemInfo
	{
		public ItemID ID { get; set; }
		public string Name { get; set; }
		public ItemCategory Category { get; set; }
		public WeaponInfo WeaponInfo { get; set; }
		public ArmorInfo ArmorInfo { get; set; }
	}

	public enum WeaponType
	{
		Edged,
		Blunt,
	}

	public class WeaponInfo
	{
		public int WC { get; set; }
		public bool IsTwoHanded { get; set; }
		public WeaponType WeaponType { get; set; }
	}

	public class ArmorInfo
	{
		public int AC { get; set; }
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
		}

		public static ItemInfo GetItemInfo(ItemID id)
		{
			Debug.Assert(id != ItemID.Undefined);
			Debug.Assert(s_items[(int)id] != null);

			return s_items[(int)id];
		}

		public static IEnumerable<ItemInfo> GetItemInfos(ItemCategory category)
		{
			Debug.Assert(category != ItemCategory.Undefined);

			return s_items.Where(ii => ii != null && ii.Category == category);
		}
	}
}
