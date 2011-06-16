using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
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

		public ulong ToUInt64()
		{
			return
				((ulong)this.FloorID << 0) |
				((ulong)this.FloorMaterialID << 8) |
				((ulong)this.InteriorID << 16) |
				((ulong)this.InteriorMaterialID << 24) |
				((ulong)this.WaterLevel << 32) |
				((this.Grass ? 1LU : 0LU) << 40) |
				((this.IsHidden ? 1LU : 0LU) << 48);
		}

		public static TileData FromUInt64(ulong value)
		{
			return new TileData()
			{
				FloorID = (FloorID)((value >> 0) & 0xff),
				FloorMaterialID = (MaterialID)((value >> 8) & 0xff),
				InteriorID = (InteriorID)((value >> 16) & 0xff),
				InteriorMaterialID = (MaterialID)((value >> 24) & 0xff),
				WaterLevel = (byte)((value >> 32) & 0xff),
				Grass = ((value >> 40) & 0xff) != 0,
				IsHidden = ((value >> 48) & 0xff) != 0,
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
