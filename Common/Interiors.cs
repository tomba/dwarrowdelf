using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public enum InteriorID : byte
	{
		Undefined,
		Empty,
		NaturalWall,
		Wall,
		Ore,
		Stairs,
		Portal,
		SlopeNorth,
		SlopeSouth,
		SlopeWest,
		SlopeEast,
		Sapling,
		Tree,
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
		public bool IsSeeThrough { get { return Blocker; } }
		public bool IsWaterPassable { get { return !Blocker; } }
	}

	public static class Interiors
	{
		static InteriorInfo[] s_interiorList;

		static Interiors()
		{
			var arr = (InteriorID[])Enum.GetValues(typeof(InteriorID));
			var max = arr.Max();
			s_interiorList = new InteriorInfo[(int)max + 1];

			foreach (var field in typeof(Interiors).GetFields())
			{
				if (field.FieldType != typeof(InteriorInfo))
					continue;

				var interInfo = (InteriorInfo)field.GetValue(null);
				s_interiorList[(int)interInfo.ID] = interInfo;
			}
		}

		public static InteriorInfo GetInterior(InteriorID id)
		{
			return s_interiorList[(int)id];
		}

		public static readonly InteriorInfo Undefined = new InteriorInfo(InteriorID.Undefined, false);

		public static readonly InteriorInfo Empty = new InteriorInfo(InteriorID.Empty, false);
		public static readonly InteriorInfo NaturalWall = new InteriorInfo(InteriorID.NaturalWall, true);
		public static readonly InteriorInfo Wall = new InteriorInfo(InteriorID.Wall, true);
		public static readonly InteriorInfo Ore = new InteriorInfo(InteriorID.Ore, true);
		public static readonly InteriorInfo Stairs = new InteriorInfo(InteriorID.Stairs, false);
		public static readonly InteriorInfo Portal = new InteriorInfo(InteriorID.Portal, false);
		public static readonly InteriorInfo SlopeNorth = new InteriorInfo(InteriorID.SlopeNorth, false);
		public static readonly InteriorInfo SlopeSouth = new InteriorInfo(InteriorID.SlopeSouth, false);
		public static readonly InteriorInfo SlopeWest = new InteriorInfo(InteriorID.SlopeWest, false);
		public static readonly InteriorInfo SlopeEast = new InteriorInfo(InteriorID.SlopeEast, false);
		public static readonly InteriorInfo Sapling = new InteriorInfo(InteriorID.Sapling, false);
		public static readonly InteriorInfo Tree = new InteriorInfo(InteriorID.Tree, true);


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
}
