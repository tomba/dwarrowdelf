using System;

namespace MyGame
{
	public static class DirectionConsts
	{
		public const int Mask = 0x3;

		public const int XShift = 0;
		public const int YShift = 2;
		public const int ZShift = 4;

		public const int DirNone = 0;
		public const int DirPos = 1;
		public const int DirNeg = 2;
	}

	public enum Direction : byte
	{
		None = 0,
		North = DirectionConsts.DirPos << DirectionConsts.YShift,
		South = DirectionConsts.DirNeg << DirectionConsts.YShift,
		West = DirectionConsts.DirNeg << DirectionConsts.XShift,
		East = DirectionConsts.DirPos << DirectionConsts.XShift,
		NorthWest = North | West,
		NorthEast = North | East,
		SouthWest = South | West,
		SouthEast = South | East,
		Up = DirectionConsts.DirPos << DirectionConsts.ZShift,
		Down = DirectionConsts.DirNeg << DirectionConsts.ZShift,
	}
}
