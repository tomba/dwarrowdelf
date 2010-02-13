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
		NaturalFloor,
		Floor,
		Hole,
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
	}

	public static class Floors
	{
		static Dictionary<FloorID, FloorInfo> s_floorMap;

		static Floors()
		{
			s_floorMap = new Dictionary<FloorID, FloorInfo>();

			foreach (var field in typeof(Floors).GetFields())
			{
				if (field.FieldType != typeof(FloorInfo))
					continue;

				var floorInfo = (FloorInfo)field.GetValue(null);
				s_floorMap[floorInfo.ID] = floorInfo;
			}
		}

		public static FloorInfo GetFloor(FloorID id)
		{
			return s_floorMap[id];
		}

		public static readonly FloorInfo Undefined = new FloorInfo(FloorID.Undefined, false, false);
		public static readonly FloorInfo Empty = new FloorInfo(FloorID.Empty, false, false);
		public static readonly FloorInfo Floor = new FloorInfo(FloorID.Floor, true, true);
		public static readonly FloorInfo NaturalFloor = new FloorInfo(FloorID.NaturalFloor, true, true);

		public static readonly FloorInfo Hole = new FloorInfo(FloorID.Hole, true, false);
	}
}
