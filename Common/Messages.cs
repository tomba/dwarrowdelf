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
	public class EnqueueActionMessage : ClientMessage
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

	[Serializable]
	public class ObjectDataMessage : ServerMessage
	{
		public BaseGameObjectData Object { get; set; }

		public override string ToString()
		{
			return String.Format("ObjectDataMessage {0}", Object.ObjectID);
		}
	}

	[Serializable]
	public class ObjectDataArrayMessage : ServerMessage
	{
		public BaseGameObjectData[] ObjectData { get; set; }
	}

	[Serializable]
	public class PropertyDataMessage : ServerMessage
	{
		public ObjectID ObjectID { get; set; }
		public PropertyID PropertyID { get; set; }
		public object Value { get; set; }

		public override string ToString()
		{
			return String.Format("PropertyDataMessage {0} {1}: {2}", this.ObjectID, this.PropertyID, this.Value);
		}
	}

	[Serializable]
	public class MapDataMessage : ServerMessage
	{
		public ObjectID Environment { get; set; }
		public VisibilityMode VisibilityMode { get; set; }
		public IntCuboid Bounds { get; set; }
	}

	[Serializable]
	public class MapDataTerrainsMessage : ServerMessage
	{
		public ObjectID Environment { get; set; }
		public IntCuboid Bounds { get; set; }
		public TileData[] TerrainData { get; set; }
	}

	[Serializable]
	public class MapDataTerrainsListMessage : ServerMessage
	{
		public ObjectID Environment { get; set; }
		public Tuple<IntPoint3D, TileData>[] TileDataList { get; set; }

		public override string ToString()
		{
			return String.Format("MapDataTerrainsListMessage({0} tiles)", this.TileDataList.Count());
		}
	}

	[Serializable]
	public class ObjectMoveMessage : ServerMessage
	{
		public ObjectID ObjectID { get; set; }
		public ObjectID TargetEnvID { get; set; }
		public IntPoint3D TargetLocation { get; set; }
		public ObjectID SourceEnvID { get; set; }
		public IntPoint3D SourceLocation { get; set; }

		public ObjectMoveMessage(IIdentifiable target, ObjectID fromID, IntPoint3D from, ObjectID toID, IntPoint3D to)
		{
			this.ObjectID = target.ObjectID;
			this.SourceEnvID = fromID;
			this.SourceLocation = from;
			this.TargetEnvID = toID;
			this.TargetLocation = to;
		}

		public override string ToString()
		{
			return String.Format("ObjectMoveMessage {0} {1}/{2}->{3}/{4}", this.ObjectID,
				this.SourceEnvID, this.SourceLocation, this.TargetEnvID, this.TargetLocation);
		}
	}

	[Serializable]
	public class ObjectDestructedMessage : ServerMessage
	{
		public ObjectID ObjectID { get; set; }

		public override string ToString()
		{
			return String.Format("ObjectDestructedMessage {0}", this.ObjectID);
		}
	}

	[Serializable]
	public class EventMessage : ServerMessage
	{
		public Event Event { get; set; }

		public EventMessage(Event @event)
		{
			this.Event = @event;
		}

		public override string ToString()
		{
			return String.Format("EventMessage({0})", this.Event);
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
