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
	public sealed class ReportMessage : InformativeMessage
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
	public sealed class IPExpressionMessage : ServerMessage
	{
		public string Script { get; private set; }

		public IPExpressionMessage(string script)
		{
			this.Script = script;
		}
	}

	[Serializable]
	public sealed class IPScriptMessage : ServerMessage
	{
		public string Script { get; private set; }
		public Tuple<string, object>[] Args { get; private set; }

		public IPScriptMessage(string script)
		{
			this.Script = script;
			this.Args = null;
		}

		public IPScriptMessage(string script, Dictionary<string, object> args)
		{
			this.Script = script;
			this.Args = args.Select(kvp => new Tuple<string, object>(kvp.Key, kvp.Value)).ToArray();
		}
	}

	[Serializable]
	public sealed class IPOutputMessage : ClientMessage
	{
		public string Text { get; set; }
	}

	[Serializable]
	public sealed class LogOnRequestMessage : ServerMessage
	{
		public string Name { get; set; }
	}

	[Serializable]
	public sealed class LogOnReplyBeginMessage : ClientMessage
	{
		public bool IsSeeAll { get; set; }
		public int Tick { get; set; }
		public LivingVisionMode LivingVisionMode { get; set; }
	}

	[Serializable]
	public sealed class LogOnReplyEndMessage : ClientMessage
	{
	}

	[Serializable]
	public sealed class LogOutRequestMessage : ServerMessage
	{
	}

	[Serializable]
	public sealed class LogOutReplyMessage : ClientMessage
	{
	}

	[Serializable]
	public sealed class EnterGameRequestMessage : ServerMessage
	{
		public string Name { get; set; }
	}

	[Serializable]
	public sealed class EnterGameReplyBeginMessage : ClientMessage
	{
	}

	[Serializable]
	public sealed class EnterGameReplyEndMessage : ClientMessage
	{
		public string ClientData { get; set; }
	}

	[Serializable]
	public sealed class ExitGameRequestMessage : ServerMessage
	{
	}

	[Serializable]
	public sealed class ExitGameReplyMessage : ClientMessage
	{
	}

	[Serializable]
	public sealed class SetWorldConfigMessage : ServerMessage
	{
		public TimeSpan? MinTickTime { get; set; }
	}

	/// <summary>
	/// StateMessages change the world state
	/// </summary>
	[Serializable]
	public abstract class StateMessage : ClientMessage
	{
	}

	[Serializable]
	public sealed class ChangeMessage : StateMessage
	{
		public ChangeData ChangeData { get; set; }

		public override string ToString()
		{
			return String.Format("ChangeMessage {0}", this.ChangeData);
		}
	}

	[Serializable]
	public sealed class ObjectDataMessage : StateMessage
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
	public sealed class MapDataTerrainsMessage : StateMessage
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
	public sealed class MapDataTerrainsListMessage : StateMessage
	{
		public ObjectID Environment { get; set; }
		public Tuple<IntPoint3, TileData>[] TileDataList { get; set; }

		public override string ToString()
		{
			return String.Format("MapDataTerrainsListMessage({0} tiles)", this.TileDataList.Count());
		}
	}

	[Serializable]
	public sealed class ControllablesDataMessage : ClientMessage
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
	public sealed class ProceedTurnRequestMessage : ClientMessage
	{
		// Sequential : Living who's turn it is
		// Simultaneous: AnyObjectID
		public ObjectID LivingID { get; set; }
	}

	[Serializable]
	public sealed class ProceedTurnReplyMessage : ServerMessage
	{
		public Tuple<ObjectID, GameAction>[] Actions { get; set; }
	}

	[Serializable]
	public sealed class SaveRequestMessage : ServerMessage
	{
	}

	[Serializable]
	public sealed class SaveClientDataRequestMessage : ClientMessage
	{
		public Guid ID;
	}

	[Serializable]
	public sealed class SaveClientDataReplyMessage : ServerMessage
	{
		public Guid ID;
		public string Data;
	}

	// Debugging / Testing messages

	[Serializable]
	public abstract class DebugMessage : ServerMessage
	{

	}

	[Serializable]
	public sealed class CreateLivingMessage : DebugMessage
	{
		public ObjectID EnvironmentID;
		public IntRectZ Area;

		public string Name;
		public LivingID LivingID;

		public bool IsControllable;
		public bool IsGroup;
	}
}
