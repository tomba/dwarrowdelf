using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.ItemMaterials
{
	public static class Materials
	{
		static Materials()
		{
			using (var stream = System.IO.File.OpenRead("ItemMaterials.xaml"))
				s_itemSet = (ItemCollection)XamlReader.Load(stream);
		}

		static ItemCollection s_itemSet;

		public static List<MaterialCollection> GetOptions(ItemType itemType)
		{
			Item item;
			if (!s_itemSet.TryGetValue(itemType, out item))
				return null;

			return item.Options;
		}

		public static bool ItemBuildableFrom(ItemType itemType, IItemObject[] obs)
		{
			var options = GetOptions(itemType);

			return options.Any(o => o.MatchItems(obs));
		}
	}

	public class ItemCollection : Dictionary<ItemType, Item>
	{
	}

	[DictionaryKeyProperty("ItemType")]
	[ContentProperty("Options")]
	public class Item
	{
		public Item()
		{
			Options = new List<MaterialCollection>();
		}

		public ItemType ItemType { get; set; }

		public List<MaterialCollection> Options { get; set; }
	}

	public class MaterialCollection : List<ItemMaterialInfo>
	{
		public bool MatchItems(IItemObject[] obs)
		{
			if (obs.Length != this.Count)
				return false;

			for (int i = 0; i < this.Count; ++i)
			{
				if (!this[i].MatchItem(obs[i]))
					return false;
			}

			return true;
		}
	}

	public class ItemMaterialInfo
	{
		public ItemType? ItemType { get; set; }
		public ItemClass? ItemClass { get; set; }

		public MaterialClass? MaterialClass { get; set; }
		public MaterialID? MaterialID { get; set; }

		public bool MatchItem(IItemObject ob)
		{
			if (this.ItemType.HasValue && this.ItemType.Value != ob.ItemID)
				return false;

			if (this.ItemClass.HasValue && this.ItemClass.Value != ob.ItemClass)
				return false;

			if (this.MaterialID.HasValue && this.MaterialID.Value != ob.MaterialID)
				return false;

			if (this.MaterialClass.HasValue && this.MaterialClass.Value != Dwarrowdelf.Materials.GetMaterial(ob.MaterialID).MaterialClass)
				return false;

			return true;
		}
	}
}
