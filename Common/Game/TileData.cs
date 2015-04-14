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
		public TerrainID TerrainID;
		[NonSerialized]
		[FieldOffset(1)]
		public MaterialID TerrainMaterialID;

		[NonSerialized]
		[FieldOffset(2)]
		public InteriorID InteriorID;
		[NonSerialized]
		[FieldOffset(3)]
		public MaterialID InteriorMaterialID;

		[NonSerialized]
		[FieldOffset(4)]
		public TileFlags Flags;

		[NonSerialized]
		[FieldOffset(5)]
		public byte WaterLevel;

		[NonSerialized]
		[FieldOffset(6)]
		public byte Unused1;

		[NonSerialized]
		[FieldOffset(7)]
		public byte Unused2;

		public const int MaxWaterLevel = 7;

		public const int SizeOf = 8;

		public static readonly TileData EmptyTileData = new TileData()
		{
			TerrainID = TerrainID.Empty,
			TerrainMaterialID = MaterialID.Undefined,
			InteriorID = InteriorID.Empty,
			InteriorMaterialID = MaterialID.Undefined,
			WaterLevel = 0,
		};

		public static readonly TileData UndefinedTileData = new TileData()
		{
			TerrainID = TerrainID.Undefined,
			TerrainMaterialID = MaterialID.Undefined,
			InteriorID = InteriorID.Undefined,
			InteriorMaterialID = MaterialID.Undefined,
		};

		public static TileData GetNaturalWall(MaterialID materialID)
		{
			return new TileData()
			{
				TerrainID = TerrainID.NaturalFloor,
				TerrainMaterialID = materialID,
				InteriorID = InteriorID.NaturalWall,
				InteriorMaterialID = materialID,
			};
		}

		public static TileData GetNaturalFloor(MaterialID materialID)
		{
			return new TileData()
			{
				TerrainID = TerrainID.NaturalFloor,
				TerrainMaterialID = materialID,
				InteriorID = InteriorID.Empty,
				InteriorMaterialID = MaterialID.Undefined,
			};
		}

		/// <summary>
		/// Is Interior a sapling or a full grown tree
		/// </summary>
		public bool HasTree
		{
			get
			{
				var id = this.InteriorID;
				return id == InteriorID.Tree || id == InteriorID.Sapling || id == InteriorID.DeadTree;
			}
		}

		/// <summary>
		/// Is Interior a full grown tree (dead or alive)
		/// </summary>
		public bool HasFellableTree
		{
			get
			{
				var id = this.InteriorID;
				return id == InteriorID.Tree || id == InteriorID.DeadTree;
			}
		}

		/// <summary>
		/// Terrain is floor
		/// </summary>
		public bool HasFloor { get { return this.TerrainID == TerrainID.NaturalFloor || this.TerrainID == TerrainID.BuiltFloor; } }

		/// <summary>
		/// Is terrain a slope
		/// </summary>
		public bool HasSlope { get { return this.TerrainID == TerrainID.Slope; } }

		/// <summary>
		/// Check if tile is TerrainID.Empty, MaterialID.Undefined, InteriorID.Empty, MaterialID.Undefined
		/// </summary>
		public bool IsEmpty { get { return this.RawTile == EmptyTileData.RawTile; } }

		/// <summary>
		/// Check if tile is TerrainID.Undefined, MaterialID.Undefined, InteriorID.Undefined, MaterialID.Undefined
		/// </summary>
		public bool IsUndefined { get { return this.RawTile == UndefinedTileData.RawTile; } }

		/// <summary>
		/// Is Interior empty or a "soft" item that can be removed automatically
		/// </summary>
		public bool IsClear
		{
			get
			{
				var id = this.InteriorID;
				return id == InteriorID.Empty || id == InteriorID.Grass || id == InteriorID.Sapling;
			}
		}

		/// <summary>
		/// Is the tile a floor with empty or soft interior
		/// </summary>
		public bool IsClearFloor { get { return this.HasFloor && this.IsClear; } }

		/// <summary>
		/// Can one see through this tile in planar directions
		/// </summary>
		public bool IsSeeThrough
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				var terrain = Terrains.GetTerrain(this.TerrainID);
				var interior = Interiors.GetInterior(this.InteriorID);

				return terrain.IsSeeThrough && interior.IsSeeThrough;
			}
		}

		/// <summary>
		/// Can one see through this tile's floor
		/// </summary>
		public bool IsSeeThroughDown
		{
			get
			{
				if (this.IsUndefined)
					throw new Exception();

				var terrain = Terrains.GetTerrain(this.TerrainID);
				return terrain.IsSeeThroughDown;
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

				var terrain = Terrains.GetTerrain(this.TerrainID);
				var interior = Interiors.GetInterior(this.InteriorID);

				return terrain.IsBlocker || interior.IsBlocker || (this.Flags & TileFlags.ItemBlocks) != 0;
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

				var terrain = Terrains.GetTerrain(this.TerrainID);

				return this.IsBlocker == false && terrain.IsSupporting;
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
		/// Water can flow through the floor
		/// </summary>
		public bool IsPermeable
		{
			get
			{
				var terrain = Terrains.GetTerrain(this.TerrainID);
				return terrain.IsPermeable;
			}
		}

		/// <summary>
		/// The tile can be mined (interior is NaturalWall or terrain is Slope)
		/// </summary>
		public bool IsMinable
		{
			get
			{
				return this.InteriorID == InteriorID.NaturalWall || this.HasSlope;
			}
		}

		/// <summary>
		/// Tile interior is a plant
		/// </summary>
		public bool IsGreen
		{
			get
			{
				switch (this.InteriorID)
				{
					case InteriorID.Grass:
					case InteriorID.Tree:
					case InteriorID.DeadTree:
					case InteriorID.Sapling:
					case InteriorID.Shrub:
						return true;

					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Tile flags has Subterranean set
		/// </summary>
		public bool IsSubterranean { get { return (this.Flags & TileFlags.Subterranean) != 0; } }
	}
}
