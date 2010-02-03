using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	public enum InteriorID : byte
	{
		Undefined,
		Empty,
		FixedWall,
		NaturalWall,
		Wall,
		Stairs,
		Portal,
		SlopeNorth,
		SlopeSouth,
		SlopeWest,
		SlopeEast,
	}

	public class InteriorInfo
	{
		public InteriorInfo(InteriorID id, bool blocker)
		{
			this.ID = id;
			this.Name = id.ToString();
			this.Blocker = blocker;
		}

		public InteriorID ID { get; private set; }
		public string Name { get; private set; }
		public bool Blocker { get; private set; }
	}

	public static class Interiors
	{
		static Dictionary<InteriorID, InteriorInfo> s_interiorMap;

		static Interiors()
		{
			s_interiorMap = new Dictionary<InteriorID, InteriorInfo>();

			foreach (var field in typeof(Interiors).GetFields())
			{
				if (field.FieldType != typeof(InteriorInfo))
					continue;

				var interInfo = (InteriorInfo)field.GetValue(null);
				s_interiorMap[interInfo.ID] = interInfo;
			}
		}

		public static InteriorInfo GetInterior(InteriorID id)
		{
			return s_interiorMap[id];
		}

		public static readonly InteriorInfo Undefined	= new InteriorInfo(InteriorID.Undefined, false);

		public static readonly InteriorInfo Empty		= new InteriorInfo(InteriorID.Empty, false);
		public static readonly InteriorInfo FixedWall	= new InteriorInfo(InteriorID.FixedWall, true);
		public static readonly InteriorInfo NaturalWall	= new InteriorInfo(InteriorID.NaturalWall, true);
		public static readonly InteriorInfo Wall		= new InteriorInfo(InteriorID.Wall, true);
		public static readonly InteriorInfo Stairs		= new InteriorInfo(InteriorID.Stairs, false);
		public static readonly InteriorInfo Portal		= new InteriorInfo(InteriorID.Portal, false);
		public static readonly InteriorInfo SlopeNorth	= new InteriorInfo(InteriorID.SlopeNorth, false);
		public static readonly InteriorInfo SlopeSouth	= new InteriorInfo(InteriorID.SlopeSouth, false);
		public static readonly InteriorInfo SlopeWest	= new InteriorInfo(InteriorID.SlopeWest, false);
		public static readonly InteriorInfo SlopeEast	= new InteriorInfo(InteriorID.SlopeEast, false);
	}

	public static class InteriorExtensions
	{
		public static bool IsSlope(this InteriorID id)
		{
			return id == InteriorID.SlopeNorth || id == InteriorID.SlopeSouth || id == InteriorID.SlopeEast || id == InteriorID.SlopeWest;
		}

		public static Direction GetDirFromSlope(InteriorID id)
		{
			switch (id)
			{
				case InteriorID.SlopeNorth:
					return Direction.North;
				case InteriorID.SlopeEast:
					return Direction.East;
				case InteriorID.SlopeSouth:
					return Direction.South;
				case InteriorID.SlopeWest:
					return Direction.West;
				default:
					throw new Exception();
			}
		}

		public static InteriorID GetSlopeFromDir(Direction dir)
		{
			switch (dir)
			{
				case Direction.North:
					return InteriorID.SlopeNorth;
				case Direction.East:
					return InteriorID.SlopeEast;
				case Direction.South:
					return InteriorID.SlopeSouth;
				case Direction.West:
					return InteriorID.SlopeWest;
				default:
					throw new Exception();
			}
		}
	}

	public enum FloorID : byte
	{
		Undefined,
		Empty,
		NaturalFloor,
		Floor,
	}

	public class FloorInfo
	{
		public FloorInfo(FloorID id, bool blocker)
		{
			this.ID = id;
			this.Name = id.ToString();
			this.Blocker = blocker;
		}

		public FloorID ID { get; private set; }
		public string Name { get; private set; }
		public bool Blocker { get; private set; }
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

		public static readonly FloorInfo Undefined	= new FloorInfo(FloorID.Undefined, false);
		public static readonly FloorInfo Empty		= new FloorInfo(FloorID.Empty, false);
		public static readonly FloorInfo Floor		= new FloorInfo(FloorID.Floor, true);
		public static readonly FloorInfo NaturalFloor	= new FloorInfo(FloorID.NaturalFloor, true);
	}


	public static class FloorExtensions
	{
		public static bool IsPassable(this FloorID id)
		{
			return id == FloorID.Empty;
		}
	}

	[DataContract]
	public struct TileData
	{
		[DataMember]
		public InteriorID InteriorID { get; set; }
		[DataMember]
		public MaterialID InteriorMaterialID { get; set; }

		[DataMember]
		public FloorID FloorID { get; set; }
		[DataMember]
		public MaterialID FloorMaterialID { get; set; }
	}

	public enum VisibilityMode
	{
		AllVisible,	// everything visible
		SimpleFOV,	// everything inside VisionRange is visible
		LOS,		// use LOS algorithm
	}
}
