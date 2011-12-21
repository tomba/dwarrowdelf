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

	/// <summary>
	/// Message from Server to Client
	/// </summary>
	[Serializable]
	public abstract class ClientMessage : Message
	{
	}

	/// <summary>
	/// Used only to give some extra information to the user
	/// </summary>
	[Serializable]
	public abstract class InformativeMessage : ClientMessage
	{
	}

	[Serializable]
	public class ReportMessage : InformativeMessage
	{
		public GameReport Report { get; set; }
	}

	/// <summary>
	/// Message from Client to Server
	/// </summary>
	[Serializable]
	public abstract class ServerMessage : Message
	{
	}

	[Serializable]
	public class IPCommandMessage : ServerMessage
	{
		public string Text { get; set; }
	}

	[Serializable]
	public class IPOutputMessage : ClientMessage
	{
		public string Text { get; set; }
	}

	[Serializable]
	public class LogOnRequestMessage : ServerMessage
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class LogOnReplyBeginMessage : ClientMessage
	{
		public bool IsSeeAll { get; set; }
		public int Tick { get; set; }
		public LivingVisionMode LivingVisionMode { get; set; }
	}

	[Serializable]
	public class LogOnReplyEndMessage : ClientMessage
	{
	}

	[Serializable]
	public class LogOutRequestMessage : ServerMessage
	{
	}

	[Serializable]
	public class LogOutReplyMessage : ClientMessage
	{
	}

	[Serializable]
	public class EnterGameRequestMessage : ServerMessage
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class EnterGameReplyBeginMessage : ClientMessage
	{
	}

	[Serializable]
	public class EnterGameReplyEndMessage : ClientMessage
	{
		public string ClientData { get; set; }
	}

	[Serializable]
	public class ExitGameRequestMessage : ServerMessage
	{
	}

	[Serializable]
	public class ExitGameReplyMessage : ClientMessage
	{
	}

	[Serializable]
	public class SetTilesMessage : ServerMessage
	{
		public ObjectID MapID { get; set; }
		public IntCuboid Cube { get; set; }

		public InteriorID? InteriorID { get; set; }
		public MaterialID? InteriorMaterialID { get; set; }

		public TerrainID? TerrainID { get; set; }
		public MaterialID? TerrainMaterialID { get; set; }

		public byte? WaterLevel { get; set; }

		public bool? Grass { get; set; }
	}

	[Serializable]
	public class CreateItemMessage : ServerMessage
	{
		public ItemID ItemID;
		public MaterialID MaterialID;

		public ObjectID EnvironmentID;
		public IntCuboid Area;
	}

	[Serializable]
	public class CreateLivingMessage : ServerMessage
	{
		public ObjectID EnvironmentID;
		public IntRectZ Area;

		public string Name;
		public LivingID LivingID;

		public bool IsControllable;
		public bool IsHerd;
	}

	[Serializable]
	public class SetWorldConfigMessage : ServerMessage
	{
		public TimeSpan? MinTickTime { get; set; }
	}

	[Serializable]
	public class CreateBuildingMessage : ServerMessage
	{
		public ObjectID MapID { get; set; }
		public IntRectZ Area { get; set; }
		public BuildingID ID { get; set; }
	}

	/// <summary>
	/// StateMessages change the world state
	/// </summary>
	[Serializable]
	public abstract class StateMessage : ClientMessage
	{
	}

	[Serializable]
	public class ChangeMessage : StateMessage
	{
		public ChangeData ChangeData { get; set; }

		public override string ToString()
		{
			return String.Format("ChangeMessage {0}", this.ChangeData);
		}
	}

	[Serializable]
	public class ObjectDataMessage : StateMessage
	{
		public BaseGameObjectData ObjectData { get; set; }

		public ObjectDataMessage(BaseGameObjectData objectData)
		{
			this.ObjectData = objectData;
		}

		public override string ToString()
		{
			return String.Format("ObjectDataMessage {0}", ObjectData.ObjectID);
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
	public class ControllablesDataMessage : ClientMessage
	{
		public enum Op
		{
			None,
			Add,
			Remove,
		}

		public Op Operation { get; set; }
		public ObjectID[] Controllables { get; set; }

		public override string ToString()
		{
			return "ControllablesDataMessage";
		}
	}

	[Serializable]
	public class ProceedTurnRequestMessage : ClientMessage
	{
		// Sequential : Living who's turn it is
		// Simultaneous: AnyObjectID
		public ObjectID LivingID { get; set; }
	}

	[Serializable]
	public class ProceedTurnReplyMessage : ServerMessage
	{
		public Tuple<ObjectID, GameAction>[] Actions { get; set; }
	}

	[Serializable]
	public class SaveRequestMessage : ServerMessage
	{
	}

	[Serializable]
	public class SaveClientDataRequestMessage : ClientMessage
	{
		public Guid ID;
	}

	[Serializable]
	public class SaveClientDataReplyMessage : ServerMessage
	{
		public Guid ID;
		public string Data;
	}
}
