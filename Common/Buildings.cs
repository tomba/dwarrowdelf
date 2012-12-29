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
		Smelter,
		Gemcutter,
	}

	[ContentProperty("BuildableItems")]
	public sealed class BuildingInfo
	{
		public BuildingID BuildingID { get; internal set; }
		public string Name { get; internal set; }
		public List<BuildableItem> BuildableItems { get; internal set; }

		public BuildingInfo()
		{
			this.BuildableItems = new List<BuildableItem>();
		}

		public BuildableItem FindBuildableItem(string buildableItemKey)
		{
			return this.BuildableItems.SingleOrDefault(i => i.Key == buildableItemKey);
		}
	}

	[ContentProperty("BuildMaterials")]
	public sealed class BuildableItem
	{
		public string Key { get; internal set; }
		public string FullKey { get; internal set; }
		public ItemID ItemID { get; internal set; }
		public ItemInfo ItemInfo { get { return Items.GetItemInfo(this.ItemID); } }
		public MaterialID? MaterialID { get; internal set; }
		public List<BuildableItemMaterialInfo> BuildMaterials { get; internal set; }
		public SkillID SkillID { get; internal set; }
		public LaborID LaborID { get; internal set; }

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
				if (!materials[i].Match(obs[i]))
					return false;
			}

			return true;
		}
	}

	public sealed class BuildableItemMaterialInfo : IItemMaterialFilter
	{
		public ItemID? ItemID { get; internal set; }
		public ItemCategory? ItemCategory { get; internal set; }

		public MaterialCategory? MaterialCategory { get; internal set; }
		public MaterialID? MaterialID { get; internal set; }

		public bool Match(IItemObject ob)
		{
			if (this.ItemID.HasValue && this.ItemID.Value != ob.ItemID)
				return false;

			if (this.ItemCategory.HasValue && this.ItemCategory.Value != ob.ItemCategory)
				return false;

			if (this.MaterialID.HasValue && this.MaterialID.Value != ob.MaterialID)
				return false;

			if (this.MaterialCategory.HasValue && this.MaterialCategory.Value != ob.MaterialCategory)
				return false;

			return true;
		}

		public IEnumerable<ItemID> ItemIDs { get { if (this.ItemID.HasValue) yield return this.ItemID.Value; } }
		public IEnumerable<ItemCategory> ItemCategories { get { if (this.ItemCategory.HasValue) yield return this.ItemCategory.Value; } }
		public IEnumerable<MaterialID> MaterialIDs { get { if (this.MaterialID.HasValue) yield return this.MaterialID.Value; } }
		public IEnumerable<MaterialCategory> MaterialCategories { get { if (this.MaterialCategory.HasValue) yield return this.MaterialCategory.Value; } }
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

			var max = buildings.Max(bi => (int)bi.BuildingID);
			s_buildings = new BuildingInfo[max + 1];

			foreach (var building in buildings)
			{
				if (s_buildings[(int)building.BuildingID] != null)
					throw new Exception();

				if (building.Name == null)
					building.Name = building.BuildingID.ToString();

				foreach (var bi in building.BuildableItems)
				{
					if (String.IsNullOrEmpty(bi.Key))
						bi.Key = bi.ItemID.ToString();

					bi.FullKey = String.Format("{0},{1}", building.BuildingID, bi.Key);
				}

				// verify BuildableItem key uniqueness
				var grouped = building.BuildableItems.GroupBy(bi => bi.Key);
				foreach (var g in grouped)
					if (g.Count() != 1)
						throw new Exception();

				s_buildings[(int)building.BuildingID] = building;
			}

			s_buildings[0] = new BuildingInfo()
			{
				BuildingID = BuildingID.Undefined,
				Name = "<undefined>",
			};
		}

		public static BuildingInfo GetBuildingInfo(BuildingID id)
		{
			Debug.Assert(id != BuildingID.Undefined);
			Debug.Assert(s_buildings[(int)id] != null);

			return s_buildings[(int)id];
		}

		public static BuildableItem FindBuildableItem(string buildableItemFullKey)
		{
			return s_buildings.SelectMany(bi => bi.BuildableItems).SingleOrDefault(bi => bi.FullKey == buildableItemFullKey);
		}
	}
}
