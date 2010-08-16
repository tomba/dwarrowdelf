using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public enum FloorID : byte
	{
		Undefined,
		Empty,
		Floor,
		Hole,
		SlopeNorth,
		SlopeSouth,
		SlopeWest,
		SlopeEast,
	}

	public class FloorInfo
	{
		public FloorInfo(FloorID id, bool isCarrying, bool isBlocking)
		{
			this.ID = id;
			this.Name = id.ToString();
			this.IsCarrying = isCarrying;
			this.IsBlocking = isBlocking;
		}

		public FloorID ID { get; private set; }
		public string Name { get; private set; }
		public bool IsCarrying { get; private set; }	/* dwarf can stand over it */
		public bool IsBlocking { get; private set; }	/* dwarf can not go through it */
		public bool IsSeeThrough { get { return IsBlocking; } }
		public bool IsWaterPassable { get { return !IsBlocking; } }
	}

	public static class Floors
	{
		static FloorInfo[] s_floorList;

		static Floors()
		{
			var arr = (FloorID[])Enum.GetValues(typeof(FloorID));
			var max = arr.Max();
			s_floorList = new FloorInfo[(int)max + 1];

			foreach (var field in typeof(Floors).GetFields())
			{
				if (field.FieldType != typeof(FloorInfo))
					continue;

				var floorInfo = (FloorInfo)field.GetValue(null);
				s_floorList[(int)floorInfo.ID] = floorInfo;
			}
		}

		public static bool IsSlope(this FloorID id)
		{
			return id == FloorID.SlopeNorth || id == FloorID.SlopeSouth || id == FloorID.SlopeEast || id == FloorID.SlopeWest;
		}

		public static FloorID ToSlope(this Direction dir)
		{
			switch (dir)
			{
				case Direction.North:
					return FloorID.SlopeNorth;
				case Direction.East:
					return FloorID.SlopeEast;
				case Direction.South:
					return FloorID.SlopeSouth;
				case Direction.West:
					return FloorID.SlopeWest;
				default:
					throw new Exception();
			}
		}

		public static Direction ToDir(this FloorID id)
		{
			switch (id)
			{
				case FloorID.SlopeNorth:
					return Direction.North;
				case FloorID.SlopeEast:
					return Direction.East;
				case FloorID.SlopeSouth:
					return Direction.South;
				case FloorID.SlopeWest:
					return Direction.West;
				default:
					throw new Exception();
			}
		}

		public static FloorInfo GetFloor(FloorID id)
		{
			return s_floorList[(int)id];
		}

		public static readonly FloorInfo Undefined = new FloorInfo(FloorID.Undefined, false, false);
		public static readonly FloorInfo Empty = new FloorInfo(FloorID.Empty, false, false);
		public static readonly FloorInfo Floor = new FloorInfo(FloorID.Floor, true, true);
		public static readonly FloorInfo SlopeNorth = new FloorInfo(FloorID.SlopeNorth, true, true);
		public static readonly FloorInfo SlopeSouth = new FloorInfo(FloorID.SlopeSouth, true, true);
		public static readonly FloorInfo SlopeWest = new FloorInfo(FloorID.SlopeWest, true, true);
		public static readonly FloorInfo SlopeEast = new FloorInfo(FloorID.SlopeEast, true, true);

		public static readonly FloorInfo Hole = new FloorInfo(FloorID.Hole, true, false);
	}
}
