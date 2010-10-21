using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;

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

	public class MaterialCollection : Dictionary<MaterialID, MaterialInfo>
	{
	}

	[DictionaryKeyProperty("ID")]
	public class MaterialInfo
	{
		string m_name;

		public MaterialID ID { get; set; }
		public MaterialClass MaterialClass { get; set; }
		public string Name
		{
			// XXX
			get { if (m_name == null) m_name = this.ID.ToString(); return m_name; }
			set { m_name = value; }
		}
		public GameColor Color { get; set; }
	}

	public static class Materials
	{
		static MaterialInfo[] s_materialList;

		static Materials()
		{
			MaterialCollection materials;

			using (var stream = System.IO.File.OpenRead("Materials.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = System.Reflection.Assembly.GetCallingAssembly(),
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					materials = (MaterialCollection)System.Xaml.XamlServices.Load(reader);
			}

			var max = (int)materials.Keys.Max();
			s_materialList = new MaterialInfo[(int)max + 1];

			foreach (var material in materials)
				s_materialList[(int)material.Key] = material.Value;

			s_materialList[0] = new MaterialInfo()
			{
				ID = MaterialID.Undefined,
				Name = "<undefined>",
				MaterialClass = MaterialClass.Undefined,
				Color = GameColor.None,
			};
		}

		public static MaterialInfo GetMaterial(MaterialID id)
		{
			return s_materialList[(int)id];
		}
	}
}
