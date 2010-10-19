using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

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
		public MaterialInfo(MaterialID id, MaterialClass materialClass, GameColor color)
		{
			this.ID = id;
			this.MaterialClass = materialClass;
			this.Name = id.ToString();
			this.Color = color;
		}

		public MaterialID ID { get; private set; }
		public MaterialClass MaterialClass { get; private set; }
		public string Name { get; private set; }
		public GameColor Color { get; private set; }
	}

	public static class Materials
	{
		static MaterialInfo[] s_materialList;

		static Materials()
		{
			var arr = (MaterialID[])Enum.GetValues(typeof(MaterialID));
			var max = arr.Max();
			s_materialList = new MaterialInfo[(int)max + 1];

			foreach (var field in typeof(Materials).GetFields())
			{
				if (field.FieldType != typeof(MaterialInfo))
					continue;

				var materialInfo = (MaterialInfo)field.GetValue(null);
				s_materialList[(int)materialInfo.ID] = materialInfo;
			}
		}

		public static MaterialInfo GetMaterial(MaterialID id)
		{
			return s_materialList[(int)id];
		}

		public static readonly MaterialInfo Undefined = new MaterialInfo(MaterialID.Undefined, MaterialClass.Undefined, GameColor.None);

		public static readonly MaterialInfo Iron = new MaterialInfo(MaterialID.Iron, MaterialClass.Metal, GameColor.SteelBlue);
		public static readonly MaterialInfo Steel = new MaterialInfo(MaterialID.Steel, MaterialClass.Metal, GameColor.LightSteelBlue);
		public static readonly MaterialInfo Diamond = new MaterialInfo(MaterialID.Diamond, MaterialClass.Gem, GameColor.LightCyan);
		public static readonly MaterialInfo Wood = new MaterialInfo(MaterialID.Wood, MaterialClass.Wood, GameColor.Sienna);
		public static readonly MaterialInfo Granite = new MaterialInfo(MaterialID.Granite, MaterialClass.Rock, GameColor.Gray);
		public static readonly MaterialInfo Gold = new MaterialInfo(MaterialID.Gold, MaterialClass.Metal, GameColor.Gold);
	}
}
