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

		public TerrainID TerrainID { get; set; }
		public MaterialID TerrainMaterialID { get; set; }

		public byte WaterLevel { get; set; }
		public const int MinWaterLevel = 1;
		public const int MaxWaterLevel = 7;
		public const int MaxCompress = 1;

		public bool Grass { get; set; }

		public ulong ToUInt64()
		{
			return
				((ulong)this.TerrainID << 0) |
				((ulong)this.TerrainMaterialID << 8) |
				((ulong)this.InteriorID << 16) |
				((ulong)this.InteriorMaterialID << 24) |
				((ulong)this.WaterLevel << 32) |
				((this.Grass ? 1LU : 0LU) << 40);
		}

		public static TileData FromUInt64(ulong value)
		{
			return new TileData()
			{
				TerrainID = (TerrainID)((value >> 0) & 0xff),
				TerrainMaterialID = (MaterialID)((value >> 8) & 0xff),
				InteriorID = (InteriorID)((value >> 16) & 0xff),
				InteriorMaterialID = (MaterialID)((value >> 24) & 0xff),
				WaterLevel = (byte)((value >> 32) & 0xff),
				Grass = ((value >> 40) & 0xff) != 0,
			};
		}
	}

	public enum VisibilityMode
	{
		/// <summary>
		/// Everything visible
		/// </summary>
		AllVisible,

		/// <summary>
		/// Areas inside the mountain are not visible
		/// </summary>
		GlobalFOV,

		/// <summary>
		/// everything inside VisionRange is visible
		/// </summary>
		SimpleFOV,

		/// <summary>
		/// use LOS algorithm
		/// </summary>
		LOS,
	}
}
