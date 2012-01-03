using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Flags]
	public enum TileFlags : ushort
	{
		None = 0,
		Grass = 1 << 0,
		ItemBlocks = 1 << 1,	// an item in the tile blocks movement
	}

	[Serializable]
	public struct TileData
	{
		public InteriorID InteriorID { get; set; }
		public MaterialID InteriorMaterialID { get; set; }

		public TerrainID TerrainID { get; set; }
		public MaterialID TerrainMaterialID { get; set; }

		public byte WaterLevel { get; set; }

		public TileFlags Flags { get; set; }

		public const int MinWaterLevel = 1;
		public const int MaxWaterLevel = 7;
		public const int MaxCompress = 1;

		public ulong ToUInt64()
		{
			return
				((ulong)this.TerrainID << 0) |
				((ulong)this.TerrainMaterialID << 8) |
				((ulong)this.InteriorID << 16) |
				((ulong)this.InteriorMaterialID << 24) |
				((ulong)this.WaterLevel << 32) |
				((ulong)this.Flags << 40);
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
				Flags = (TileFlags)((value >> 40) & 0xffff),
			};
		}
	}
}
