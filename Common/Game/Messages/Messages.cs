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
		public GameReport Report;
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
		public string Script;

		public IPExpressionMessage(string script)
		{
			this.Script = script;
		}
	}

	[Serializable]
	public sealed class IPScriptMessage : ServerMessage
	{
		public string Script;
		public KeyValuePair<string, object>[] Args;

		public IPScriptMessage(string script)
		{
			this.Script = script;
			this.Args = null;
		}

		public IPScriptMessage(string script, Dictionary<string, object> args)
		{
			this.Script = script;
			this.Args = args.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value)).ToArray();
		}
	}

	[Serializable]
	public sealed class IPOutputMessage : ClientMessage
	{
		public string Text;
	}

	[Serializable]
	public sealed class LogOnRequestMessage : ServerMessage
	{
		public string Name;
	}

	[Serializable]
	public sealed class LogOnReplyBeginMessage : ClientMessage
	{
		public int PlayerID;
		public bool IsSeeAll;
		public GameMode GameMode;
	}

	[Serializable]
	public sealed class LogOnReplyEndMessage : ClientMessage
	{
	}

	[Serializable]
	public sealed class ClientDataMessage : ClientMessage
	{
		public string ClientData;
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
	public sealed class SetWorldConfigMessage : ServerMessage
	{
		public TimeSpan? MinTickTime;
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
		public ChangeData ChangeData;

		public override string ToString()
		{
			return String.Format("ChangeMessage {0}", this.ChangeData);
		}
	}

	[Serializable]
	public sealed class WorldDataMessage : StateMessage
	{
		public WorldData WorldData;

		public WorldDataMessage(WorldData worldData)
		{
			this.WorldData = worldData;
		}

		public override string ToString()
		{
			return String.Format("WorldDataMessage");
		}
	}

	[Serializable]
	public sealed class ObjectDataMessage : StateMessage
	{
		public BaseGameObjectData ObjectData;

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
	public sealed class ObjectDataEndMessage : StateMessage
	{
		public ObjectID ObjectID;

		public override string ToString()
		{
			return String.Format("ObjectDataEndMessage {0}", this.ObjectID);
		}
	}

	[Serializable]
	public sealed class MapDataTerrainsMessage : StateMessage
	{
		public ObjectID Environment;
		public IntGrid3 Bounds;
		public bool IsTerrainDataCompressed;
		public byte[] TerrainData;

		public override string ToString()
		{
			return String.Format("MapDataTerrainsMessage {0}, bounds {1}", this.Environment, this.Bounds);
		}
	}

	[Serializable]
	public sealed class MapDataTerrainsListMessage : StateMessage
	{
		public ObjectID Environment;
		public KeyValuePair<IntPoint3, TileData>[] TileDataList;

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

		public Op Operation;
		public ObjectID[] Controllables;

		public override string ToString()
		{
			return "ControllablesDataMessage";
		}
	}

	[Serializable]
	public sealed class ProceedTurnReplyMessage : ServerMessage
	{
		public KeyValuePair<ObjectID, GameAction>[] Actions;
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
}
