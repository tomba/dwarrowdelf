using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Markup;

namespace Dwarrowdelf
{
	public enum BuildingID : byte
	{
		Undefined = 0,
		Carpenter,
		Mason,
		Smith,
	}

	[ContentProperty("BuildableItems")]
	public class BuildingInfo
	{
		public BuildingID ID { get; set; }
		public string Name { get; set; }
		public List<BuildableItem> BuildableItems { get; set; }

		public BuildingInfo()
		{
			this.BuildableItems = new List<BuildableItem>();
		}

		public BuildableItem FindBuildableItem(ItemType itemType)
		{
			return this.BuildableItems.SingleOrDefault(i => i.ItemType == itemType);
		}

		public bool ItemBuildableFrom(ItemType itemType, IItemObject[] obs)
		{
			var item = FindBuildableItem(itemType);

			if (item == null)
				throw new Exception();

			return item.MatchItems(obs);
		}
	}

	[ContentProperty("BuildMaterials")]
	public class BuildableItem
	{
		public ItemType ItemType { get; set; }
		public List<BuildableItemMaterialInfo> BuildMaterials { get; set; }

		public BuildableItem()
		{
			BuildMaterials = new List<BuildableItemMaterialInfo>();
		}

		public bool MatchItems(IItemObject[] obs)
		{
			var materials = this.BuildMaterials;

			if (obs.Length != materials.Count)
				return false;

			for (int i = 0; i < materials.Count; ++i)
			{
				if (!materials[i].MatchItem(obs[i]))
					return false;
			}

			return true;
		}
	}

	public class BuildableItemMaterialInfo
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

	public static class Buildings
	{
		static BuildingInfo[] s_buildings;

		static Buildings()
		{
			BuildingInfo[] buildings;

			using (var stream = System.IO.File.OpenRead("Buildings.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = System.Reflection.Assembly.GetCallingAssembly(),
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					buildings = (BuildingInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = buildings.Max(bi => (int)bi.ID);
			s_buildings = new BuildingInfo[max + 1];

			foreach (var building in buildings)
			{
				if (s_buildings[(int)building.ID] != null)
					throw new Exception();

				if (building.Name == null)
					building.Name = building.ID.ToString();

				s_buildings[(int)building.ID] = building;
			}

			s_buildings[0] = new BuildingInfo()
			{
				ID = BuildingID.Undefined,
				Name = "<undefined>",
			};
		}

		public static BuildingInfo GetBuildingInfo(BuildingID id)
		{
			Debug.Assert(id != BuildingID.Undefined);
			Debug.Assert(s_buildings[(int)id] != null);

			return s_buildings[(int)id];
		}
	}
}
