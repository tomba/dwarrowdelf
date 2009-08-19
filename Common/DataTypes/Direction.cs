using System;

namespace MyGame
{
	public enum Direction : byte
	{
		None = 0,
		North = DirNeg << YShift,
		South = DirPos << YShift,
		West = DirNeg << XShift,
		East = DirPos << XShift,
		NorthWest = (DirNeg << YShift) | (DirNeg << XShift),
		NorthEast = (DirNeg << YShift) | (DirPos << XShift),
		SouthWest = (DirPos << YShift) | (DirNeg << XShift),
		SouthEast = (DirPos << YShift) | (DirPos << XShift),
		Up = DirPos << ZShift,
		Down = DirNeg << ZShift,

		Mask = 0x3,
		XMask = 0x3,
		YMask = 0x3 << 2,
		ZMask = 0x3 << 4,

		XShift = 0,
		YShift = 2,
		ZShift = 4,

		DirNone = 0,
		DirPos = 1,
		DirNeg = 2,
	}
}
