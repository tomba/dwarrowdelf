using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(TileDataConverter))]
	public struct TileData
	{
		public InteriorID InteriorID { get; set; }
		public MaterialID InteriorMaterialID { get; set; }

		public FloorID FloorID { get; set; }
		public MaterialID FloorMaterialID { get; set; }

		public byte WaterLevel { get; set; }
		public const int MinWaterLevel = 1;
		public const int MaxWaterLevel = 7;
		public const int MaxCompress = 1;

		public bool Grass { get; set; }

		public bool HasGrass { get { return this.Grass; } }
		public bool IsEmpty { get { return this.InteriorID == InteriorID.Empty && this.FloorID == FloorID.Empty; } }

		public bool IsHidden { get; set; }

		internal string ConvertToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2},{3},{4},{5},{6}", this.FloorID, this.FloorMaterialID, this.InteriorID, this.InteriorMaterialID,
				this.WaterLevel, this.Grass, this.IsHidden);
		}

		public static TileData Parse(string str)
		{
			var arr = str.Split(',');
			return new TileData()
			{
				FloorID = (FloorID)Enum.Parse(typeof(FloorID), arr[0]),
				FloorMaterialID = (MaterialID)Enum.Parse(typeof(MaterialID), arr[1]),
				InteriorID = (InteriorID)Enum.Parse(typeof(InteriorID), arr[2]),
				InteriorMaterialID = (MaterialID)Enum.Parse(typeof(MaterialID), arr[3]),
				WaterLevel = byte.Parse(arr[4]),
				Grass = bool.Parse(arr[5]),
				IsHidden = bool.Parse(arr[6]),
			};
		}
	}

	public enum VisibilityMode
	{
		AllVisible,	// everything visible
		GlobalFOV,	// areas inside the mountain are not visible
		SimpleFOV,	// everything inside VisionRange is visible
		LOS,		// use LOS algorithm
	}
}
