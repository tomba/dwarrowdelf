using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public enum MaterialID : byte
	{
		Undefined,
		Granite,
		Iron,
		Steel,
		Diamond,
		Wood,
		Gold,
	}

	public enum MaterialClass : byte
	{
		Undefined,
		Wood,
		Rock,
		Metal,
		Gem,
	}

	public class MaterialInfo
	{
		public MaterialID ID { get; set; }
		public MaterialClass MaterialClass { get; set; }
		public string Name { get; set; }
		public GameColor Color { get; set; }
	}

	public static class Materials
	{
		static MaterialInfo[] s_materials;

		static Materials()
		{
			MaterialInfo[] materials;

			using (var stream = System.IO.File.OpenRead("Materials.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = System.Reflection.Assembly.GetCallingAssembly(),
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					materials = (MaterialInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = materials.Max(m => (int)m.ID);
			s_materials = new MaterialInfo[max + 1];

			foreach (var item in materials)
			{
				if (s_materials[(int)item.ID] != null)
					throw new Exception();

				if (item.Name == null)
					item.Name = item.ID.ToString();

				s_materials[(int)item.ID] = item;
			}

			s_materials[0] = new MaterialInfo()
			{
				ID = MaterialID.Undefined,
				Name = "<undefined>",
				MaterialClass = MaterialClass.Undefined,
				Color = GameColor.None,
			};
		}

		public static MaterialInfo GetMaterial(MaterialID id)
		{
			Debug.Assert(s_materials[(int)id] != null);

			return s_materials[(int)id];
		}
	}
}
