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
		Undefined,

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
		Loam,

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

		// Other
		Flesh,
		Water,
	}

	public enum MaterialCategory
	{
		Undefined,
		Wood,
		Rock,
		Soil,
		Metal,
		Gem,
		Mineral,
		Grass,
		Consumable,
	}

	public enum WoodMaterialCategory
	{
		Undefined,
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

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Data.Materials.xaml"))
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
	}
}
