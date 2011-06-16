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

		public bool IsEmpty { get { return this.InteriorID == InteriorID.Empty && this.FloorID == FloorID.Empty; } }

		public bool IsHidden { get; set; }

#if VERBOSE
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
#else
		internal string ConvertToString()
		{
			ulong v =
				((ulong)this.FloorID << 0) |
				((ulong)this.FloorMaterialID << 8) |
				((ulong)this.InteriorID << 16) |
				((ulong)this.InteriorMaterialID << 24) |
				((ulong)this.WaterLevel << 32) |
				((this.Grass ? 1LU : 0LU) << 40) |
				((this.IsHidden ? 1LU : 0LU) << 48);

			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return v.ToString(info);
		}

		public static TileData Parse(string str)
		{
			ulong v = ulong.Parse(str);

			return new TileData()
			{
				FloorID = (FloorID)((v >> 0) & 0xff),
				FloorMaterialID = (MaterialID)((v >> 8) & 0xff),
				InteriorID = (InteriorID)((v >> 16) & 0xff),
				InteriorMaterialID = (MaterialID)((v >> 24) & 0xff),
				WaterLevel = (byte)((v >> 32) & 0xff),
				Grass = ((v >> 40) & 0xff) != 0,
				IsHidden = ((v >> 48) & 0xff) != 0,
			};
		}
#endif
	}

	public enum VisibilityMode
	{
		AllVisible,	// everything visible
		GlobalFOV,	// areas inside the mountain are not visible
		SimpleFOV,	// everything inside VisionRange is visible
		LOS,		// use LOS algorithm
	}
}
