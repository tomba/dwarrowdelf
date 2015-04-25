using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace Dwarrowdelf
{
	/// <summary>
	/// Maintained by EnvironmentObject internally.
	/// The flags are helper flags, they can be deduced from other data.
	/// </summary>
	[Flags]
	public enum TileFlags : byte
	{
		None = 0,
		ItemBlocks = 1 << 0,	// an item in the tile blocks movement
		Subterranean = 1 << 1,
		WaterStatic = 1 << 2,	// Water in the tile is static
		HasSupport = 1 << 3,	// Tile below supports, so this one can be stood upon
		HasWallBelow = 1 << 4,	// Tile below is a Wall, so this one can be stood and built upon, and has a "floor"
		Error = 1 << 7,			// ZZZ: flags are undefined (in terrain gen), remove
	}

	//     0         1       2         3        4       5        6        7
	// |TerID   |TerMatID|IntID   |IntMatID|Flags   |Water   |        |        |

	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct TileData
	{
		[FieldOffset(0)]
		public ulong Raw;
		[NonSerialized]
		[FieldOffset(0)]
		public uint RawTile;	// contains only terrain & interior data

		[NonSerialized]
		[FieldOffset(0)]
		public TileID ID;
		[NonSerialized]
		[FieldOffset(1)]
		public MaterialID MaterialID;

		[NonSerialized]
		[FieldOffset(2)]
		public MaterialID SecondaryMaterialID;
		[NonSerialized]
		[FieldOffset(3)]
		public byte Unused1;

		[NonSerialized]
		[FieldOffset(4)]
		public TileFlags Flags;

		[NonSerialized]
		[FieldOffset(5)]
		public byte WaterLevel;

		[NonSerialized]
		[FieldOffset(6)]
		public byte Unused2;

		[NonSerialized]
		[FieldOffset(7)]
		public byte Unused3;

		public const int MaxWaterLevel = 7;

		public const int SizeOf = 8;

		public static readonly TileData EmptyTileData = new TileData()
		{
			ID = TileID.Empty,
			MaterialID = MaterialID.Undefined,
		};

		public static readonly TileData UndefinedTileData = new TileData()
		{
			ID = TileID.Undefined,
			MaterialID = MaterialID.Undefined,
		};

		public static TileData GetNaturalWall(MaterialID materialID)
		{
			return new TileData() { ID = TileID.NaturalWall, MaterialID = materialID };
		}

		/// <summary>
		/// Is tile a sapling or a full grown tree
		/// </summary>
		public bool HasTree
		{
			get
			{
				var id = this.ID;
				return id == TileID.Tree || id == TileID.Sapling || id == TileID.DeadTree;
			}
		}

		/// <summary>
		/// Is Interior a full grown tree (dead or alive)
		/// </summary>
		public bool HasFellableTree
		{
			get
			{
				var id = this.ID;
				return id == TileID.Tree || id == TileID.DeadTree;
			}
		}

		/// <summary>
		/// Tile below is supporting
		/// </summary>
		public bool HasFloor
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				if ((this.Flags & TileFlags.Error) != 0)
					System.Diagnostics.Debugger.Break();

				return this.HasWallBelow;
			}
		}

		public bool HasSupportBelow
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				if ((this.Flags & TileFlags.Error) != 0)
					System.Diagnostics.Debugger.Break();

				return (this.Flags & TileFlags.HasSupport) != 0;
			}
		}

		/// <summary>
		/// Check if tile is empty
		/// </summary>
		public bool IsEmpty { get { return this.ID == TileID.Empty; } }

		/// <summary>
		/// Check if tile is undefined
		/// </summary>
		public bool IsUndefined { get { return this.ID == TileID.Undefined; } }

		/// <summary>
		/// Is Interior empty or a "soft" item that can be removed automatically
		/// </summary>
		public bool IsClear
		{
			get
			{
				switch (this.ID)
				{
					case TileID.Empty:
					case TileID.Grass:
					case TileID.Sapling:
						return true;

					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Is the tile a floor with empty or soft interior
		/// </summary>
		public bool IsClearFloor
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				return this.HasFloor && this.IsClear;
			}
		}

		/// <summary>
		/// Can one see through this tile in planar directions
		/// </summary>
		public bool IsSeeThrough
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				switch (this.ID)
				{
					case TileID.NaturalWall:
					case TileID.BuiltWall:
						return false;

					default:
						return true;
				}
			}
		}

		/// <summary>
		/// Does the terrain, interior, or an item block entry
		/// </summary>
		public bool IsBlocker
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				if ((this.Flags & TileFlags.ItemBlocks) != 0)
					return true;

				switch (this.ID)
				{
					case TileID.NaturalWall:
					case TileID.BuiltWall:
					case TileID.Tree:
					case TileID.DeadTree:
						return true;

					default:
						return false;
				}
			}
		}

		/// <summary>
		/// The tile does not block, and can be stood upon
		/// </summary>
		public bool IsWalkable
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				return this.HasSupportBelow && !this.IsBlocker;
			}
		}

		/// <summary>
		/// Provides support to above tile, i.e. you can walk above this tile
		/// </summary>
		public bool IsSupporting
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				if (this.IsWall)
					return true;

				switch (this.ID)
				{
					case TileID.Stairs:
						return true;

					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Water can enter the tile
		/// </summary>
		public bool IsWaterPassable
		{
			get
			{
				// this should later be changed to handle bars etc.
				return this.IsBlocker == false;
			}
		}

		/// <summary>
		/// The tile can be mined (interior is NaturalWall)
		/// </summary>
		public bool IsMinable
		{
			get
			{
				switch (this.ID)
				{
					case TileID.NaturalWall:
						return true;

					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Tile interior is a plant
		/// </summary>
		public bool IsGreen
		{
			get
			{
				if (this.HasTree)
					return true;

				switch (this.ID)
				{
					case TileID.Grass:
					case TileID.Shrub:
						return true;

					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Tile flags has Subterranean set
		/// </summary>
		public bool IsSubterranean
		{
			get
			{
				if ((this.Flags & TileFlags.Error) != 0)
					System.Diagnostics.Debugger.Break();

				return (this.Flags & TileFlags.Subterranean) != 0;
			}
		}

		/// <summary>
		/// Tile is a Wall
		/// </summary>
		public bool IsWall
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				switch (this.ID)
				{
					case TileID.NaturalWall:
					case TileID.BuiltWall:
						return true;

					default:
						return false;
				}
			}
		}

		/// <summary>
		/// The tile below is a Wall
		/// </summary>
		public bool HasWallBelow
		{
			get
			{
				if ((this.Flags & TileFlags.Error) != 0)
					System.Diagnostics.Debugger.Break();

				return (this.Flags & TileFlags.HasWallBelow) != 0;
			}
		}
	}
}
