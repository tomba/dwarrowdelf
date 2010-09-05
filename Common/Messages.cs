using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame.Messages
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
	public class DoActionMessage : ClientMessage
	{
		public GameAction Action { get; set; }
	}

	[Serializable]
	public class ProceedTickMessage : ClientMessage
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
	public class CreateBuildingMessage : ClientMessage
	{
		public ObjectID MapID { get; set; }
		public IntRect Area { get; set; }
		public int Z { get; set; }
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
	}

	[Serializable]
	public class MapDataMessage : StateMessage
	{
		public ObjectID Environment { get; set; }
		public VisibilityMode VisibilityMode { get; set; }
		public IntCuboid Bounds { get; set; }
	}

	[Serializable]
	public class MapDataTerrainsMessage : StateMessage
	{
		public ObjectID Environment { get; set; }
		public IntCuboid Bounds { get; set; }
		public TileData[] TerrainData { get; set; }
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
	public class ActionRequiredMessage : ServerMessage
	{
		public ObjectID ObjectID { get; set; }

		public override string ToString()
		{
			return String.Format("ActionRequiredMessage({0})", this.ObjectID);
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
}
