using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Markup;

namespace Dwarrowdelf
{
	public enum BuildingID
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

		public BuildableItem FindBuildableItem(ItemID itemID)
		{
			return this.BuildableItems.SingleOrDefault(i => i.ItemID == itemID);
		}

		public bool ItemBuildableFrom(ItemID itemID, IItemObject[] obs)
		{
			var item = FindBuildableItem(itemID);

			if (item == null)
				throw new Exception();

			return item.MatchItems(obs);
		}
	}

	[ContentProperty("BuildMaterials")]
	public class BuildableItem
	{
		public ItemID ItemID { get; set; }
		public ItemInfo ItemInfo { get { return Items.GetItem(this.ItemID); } }
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
		public ItemID? ItemID { get; set; }
		public ItemClass? ItemClass { get; set; }

		public MaterialClass? MaterialClass { get; set; }
		public MaterialID? MaterialID { get; set; }

		public bool MatchItem(IItemObject ob)
		{
			if (this.ItemID.HasValue && this.ItemID.Value != ob.ItemID)
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
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			BuildingInfo[] buildings;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Data.Buildings.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = asm,
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
