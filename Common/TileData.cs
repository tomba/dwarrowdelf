using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

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
	[StructLayout(LayoutKind.Explicit)]
	public struct TileData
	{
		[FieldOffset(0)]
		public ulong Raw;

		[FieldOffset(0)]
		public TerrainID TerrainID;
		[FieldOffset(1)]
		public MaterialID TerrainMaterialID;

		[FieldOffset(2)]
		public InteriorID InteriorID;
		[FieldOffset(3)]
		public MaterialID InteriorMaterialID;

		[FieldOffset(4)]
		public TileFlags Flags;

		[FieldOffset(6)]
		public byte WaterLevel;

		public bool HasGrass { get { return (this.Flags & TileFlags.Grass) != 0; } }

		public const int MinWaterLevel = 1;
		public const int MaxWaterLevel = 7;
		public const int MaxCompress = 1;
	}
}
