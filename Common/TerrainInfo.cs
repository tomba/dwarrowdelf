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
		NaturalWall,
		Wall,
		StairsUp,
		StairsDown,
		Portal,
	}

	public enum FloorID : byte
	{
		Undefined,
		Empty,
		NaturalFloor,
		Floor,
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

	public class TerrainInfo
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public int SymbolID { get; set; }
		public bool IsWalkable { get; set; }
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

	public class Terrains
	{
		Dictionary<InteriorID, InteriorInfo> m_interiorMap = new Dictionary<InteriorID,InteriorInfo>();
		Dictionary<FloorID, FloorInfo> m_floorMap = new Dictionary<FloorID, FloorInfo>();

		public Terrains()
		{
			Add(new InteriorInfo(InteriorID.Undefined, false));

			Add(new InteriorInfo(InteriorID.Empty, false));
			Add(new InteriorInfo(InteriorID.NaturalWall, true));
			Add(new InteriorInfo(InteriorID.Wall, true));
			Add(new InteriorInfo(InteriorID.StairsDown, false));
			Add(new InteriorInfo(InteriorID.StairsUp, false));
			Add(new InteriorInfo(InteriorID.Portal, false));

			Add(new FloorInfo(FloorID.Undefined, false));

			Add(new FloorInfo(FloorID.Empty, false));
			Add(new FloorInfo(FloorID.Floor, true));
			Add(new FloorInfo(FloorID.NaturalFloor, true));
		}

		void Add(InteriorInfo info)
		{
			m_interiorMap[info.ID] = info;
		}

		void Add(FloorInfo info)
		{
			m_floorMap[info.ID] = info;
		}

		public InteriorInfo GetInteriorInfo(InteriorID id)
		{
			return m_interiorMap[id];
		}

		public FloorInfo GetFloorInfo(FloorID id)
		{
			return m_floorMap[id];
		}
	}
}
