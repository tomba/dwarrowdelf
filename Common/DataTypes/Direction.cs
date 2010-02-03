using System;
using System.Collections.Generic;

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

	public static class DirectionExtensions
	{
		public static Direction Reverse(this Direction dir)
		{
			// XXX optimize
			return new IntVector3D(dir).Reverse().ToDirection();
		}

		public static IEnumerable<Direction> GetCardinalDirections()
		{
			yield return Direction.North;
			yield return Direction.East;
			yield return Direction.South;
			yield return Direction.West;
		}

		public static IEnumerable<Direction> GetIntercardinalDirections()
		{
			yield return Direction.NorthEast;
			yield return Direction.SouthEast;
			yield return Direction.SouthWest;
			yield return Direction.NorthWest;
		}
	}
}
