using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf.Messages
{
	[Serializable]
	public abstract class Message
	{
	}

	[Serializable]
	public abstract class ServerMessage : Message
	{
	}

	[Serializable]
	public abstract class InformativeMessage : ServerMessage
	{
	}

	[Serializable]
	public abstract class ClientMessage : Message
	{
	}

	[Serializable]
	public class IPCommandMessage : ClientMessage
	{
		public string Text { get; set; }
	}

	[Serializable]
	public class IPOutputMessage : ServerMessage
	{
		public string Text { get; set; }
	}

	[Serializable]
	public class LogOnRequestMessage : ClientMessage
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class LogOnReplyMessage : ServerMessage
	{
		public int UserID { get; set; }
		public bool IsSeeAll { get; set; }
	}

	[Serializable]
	public class LogOffRequestMessage : ClientMessage
	{
	}

	[Serializable]
	public class LogOffReplyMessage : ServerMessage
	{
	}

	[Serializable]
	public class LogOnCharRequestMessage : ClientMessage
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class LogOnCharReplyMessage : ServerMessage
	{
	}

	[Serializable]
	public class LogOffCharRequestMessage : ClientMessage
	{
	}

	[Serializable]
	public class LogOffCharReplyMessage : ServerMessage
	{
	}

	[Serializable]
	public class SetTilesMessage : ClientMessage
	{
		public ObjectID MapID { get; set; }
		public IntCuboid Cube { get; set; }

		public InteriorID? InteriorID { get; set; }
		public MaterialID? InteriorMaterialID { get; set; }

		public FloorID? FloorID { get; set; }
		public MaterialID? FloorMaterialID { get; set; }

		public byte? WaterLevel { get; set; }

		public bool? Grass { get; set; }
	}

	[Serializable]
	public class SetWorldConfigMessage : ClientMessage
	{
		public TimeSpan? MinTickTime { get; set; }
	}

	[Serializable]
	public class CreateBuildingMessage : ClientMessage
	{
		public ObjectID MapID { get; set; }
		public IntRect3D Area { get; set; }
		public BuildingID ID { get; set; }
	}

	/// <summary>
	/// StateMessages change the world state
	/// </summary>
	[Serializable]
	public abstract class StateMessage : ServerMessage
	{
	}

	[Serializable]
	public class ChangeMessage : StateMessage
	{
		public Change Change { get; set; }

		public override string ToString()
		{
			return String.Format("ChangeMessage {0}", this.Change);
		}
	}

	[Serializable]
	public class ObjectDataMessage : StateMessage
	{
		public BaseGameObjectData ObjectData { get; set; }

		public override string ToString()
		{
			return String.Format("ObjectDataMessage {0}", ObjectData.ObjectID);
		}
	}

	[Serializable]
	public class ObjectDataArrayMessage : StateMessage
	{
		public BaseGameObjectData[] ObjectDatas { get; set; }

		public override string ToString()
		{
			return String.Format("ObjectDataArrayMessage for {0}",
				String.Join(", ", this.ObjectDatas.Select(d => d.ObjectID.ToString())));
		}
	}

	[Serializable]
	public class MapDataStart : InformativeMessage
	{
		public ObjectID Environment { get; set; }
	}

	[Serializable]
	public class MapDataEnd : InformativeMessage
	{
		public ObjectID Environment { get; set; }
	}

	[Serializable]
	public class MapDataMessage : StateMessage
	{
		public ObjectID Environment { get; set; }
		public VisibilityMode VisibilityMode { get; set; }
		public IntCuboid Bounds { get; set; }

		public override string ToString()
		{
			return String.Format("MapDataMessage {0}, bounds {1}", this.Environment, this.Bounds);
		}
	}

	[Serializable]
	public class MapDataTerrainsMessage : StateMessage
	{
		public ObjectID Environment { get; set; }
		public IntCuboid Bounds { get; set; }
		public TileData[] TerrainData { get; set; }

		public override string ToString()
		{
			return String.Format("MapDataTerrainsMessage {0}, bounds {1}", this.Environment, this.Bounds);
		}
	}

	[Serializable]
	public class MapDataTerrainsListMessage : StateMessage
	{
		public ObjectID Environment { get; set; }
		public Tuple<IntPoint3D, TileData>[] TileDataList { get; set; }

		public override string ToString()
		{
			return String.Format("MapDataTerrainsListMessage({0} tiles)", this.TileDataList.Count());
		}
	}

	[Serializable]
	public class ControllablesDataMessage : ServerMessage
	{
		public ObjectID[] Controllables { get; set; }

		public override string ToString()
		{
			return "ControllablesDataMessage";
		}
	}

	[Serializable]
	public class ProceedTurnMessage : ClientMessage
	{
		public Tuple<ObjectID, GameAction>[] Actions { get; set; }
	}

}
