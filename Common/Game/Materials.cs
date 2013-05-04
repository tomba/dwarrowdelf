using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Diagnostics;

namespace Dwarrowdelf
{
	// Stored in TileData, needs to be byte
	public enum MaterialID : byte
	{
		Undefined = 0,

		// Alloys
		Steel,
		Bronze,
		Brass,

		// Pure Metals
		Iron,
		Gold,
		Silver,
		Platinum,
		Copper,
		Zinc,
		Lead,
		Tin,

		// Rocks
		Granite,
		Quartzite,
		Sandstone,
		Diorite,
		Dolostone,	// mostly Dolomite

		// Soils
		Sand,
		LoamySand,
		SandyLoam,
		SandyClayLoam,
		SandyClay,
		Clay,
		SiltyClay,
		SiltyClayLoam,
		SiltLoam,
		Silt,
		Loam,
		ClayLoam,

		// Minerals
		Magnetite,
		NativeGold,
		NativeSilver,
		NativePlatinum,

		// Gems (minerals)
		Diamond,
		Ruby,
		Sapphire,
		Emerald,
		Chrysoprase,

		// Woods
		Oak,
		Birch,
		Fir,
		Pine,

		// Grass
		ReedGrass,
		MeadowGrass,
		HairGrass,
		RyeGrass,

		// Berries
		Blackberry,
		Elderberry,
		Gooseberry,

		// Other
		Flesh,
		Water,
	}

	[Serializable]
	public sealed class MaterialIDMask : EnumBitMask64<MaterialID>
	{
		public MaterialIDMask() : base() { }
		public MaterialIDMask(MaterialID materialID) : base(materialID) { }
		public MaterialIDMask(IEnumerable<MaterialID> materialIDs) : base(materialIDs) { }
	}

	public enum MaterialCategory
	{
		Undefined = 0,

		Wood,
		Rock,
		Soil,
		Metal,
		Gem,
		Mineral,
		Grass,
		Consumable,
		Berry,
	}

	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(MaterialCategoryMaskConverter))]
	public sealed class MaterialCategoryMask : EnumBitMask32<MaterialCategory>
	{
		public MaterialCategoryMask() : base() { }
		public MaterialCategoryMask(MaterialCategory category) : base(category) { }
		public MaterialCategoryMask(IEnumerable<MaterialCategory> categories) : base(categories) { }
	}

	public enum WoodMaterialCategory
	{
		Undefined = 0,

		Coniferous,
		Deciduous,
	}

	public sealed class MaterialInfo
	{
		public MaterialID ID { get; internal set; }
		public MaterialCategory Category { get; internal set; }
		public string Name { get; internal set; }
		public string Adjective { get; internal set; }
		public GameColor Color { get; internal set; }
	}

	public static class Materials
	{
		static MaterialInfo[] s_materials;

		static Materials()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			MaterialInfo[] materials;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Game.Materials.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = asm,
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					materials = (MaterialInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = materials.Max(m => (int)m.ID);
			s_materials = new MaterialInfo[max + 1];

			foreach (var item in materials)
			{
				if (s_materials[(int)item.ID] != null)
					throw new Exception("Duplicate entry");

				if (item.Name == null)
					item.Name = item.ID.ToString().ToLowerInvariant();

				if (item.Adjective == null)
					item.Adjective = item.Name;

				s_materials[(int)item.ID] = item;
			}

			s_materials[0] = new MaterialInfo()
			{
				ID = MaterialID.Undefined,
				Name = "<undefined>",
				Category = MaterialCategory.Undefined,
				Color = GameColor.None,
			};
		}

		public static MaterialInfo GetMaterial(MaterialID id)
		{
			Debug.Assert(s_materials[(int)id] != null);

			return s_materials[(int)id];
		}

		public static IEnumerable<MaterialInfo> GetMaterials()
		{
			return s_materials.Where(m => m != null);
		}

		public static IEnumerable<MaterialInfo> GetMaterials(MaterialCategory materialClass)
		{
			return s_materials.Where(m => m != null && m.Category == materialClass);
		}

		public static WoodMaterialCategory GetWoodMaterialCategory(MaterialID materialID)
		{
			switch (materialID)
			{
				case MaterialID.Fir:
				case MaterialID.Pine:
					return WoodMaterialCategory.Coniferous;

				case MaterialID.Birch:
				case MaterialID.Oak:
					return WoodMaterialCategory.Deciduous;

				default:
					throw new Exception();
			}
		}

		public static IEnumerable<MaterialID> GetMaterialIDs()
		{
			return EnumHelpers.GetEnumValues<MaterialID>().Skip(1);
		}

		public static IEnumerable<MaterialCategory> GetMaterialCategories()
		{
			return EnumHelpers.GetEnumValues<MaterialCategory>().Skip(1);
		}
	}
}
