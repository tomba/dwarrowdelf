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
	public class IPCommandMessage : Message
	{
		public string Text { get; set; }
	}

	[Serializable]
	public class IPOutputMessage : Message
	{
		public string Text { get; set; }
	}

	[Serializable]
	public class LogOnRequestMessage : Message
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class LogOnReplyMessage : Message
	{
		public int UserID { get; set; }
		public bool IsSeeAll { get; set; }
	}

	[Serializable]
	public class LogOffRequestMessage : Message
	{
	}

	[Serializable]
	public class LogOffReplyMessage : Message
	{
	}

	[Serializable]
	public class LogOnCharRequestMessage : Message
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class LogOnCharReplyMessage : Message
	{
	}

	[Serializable]
	public class LogOffCharRequestMessage : Message
	{
	}

	[Serializable]
	public class LogOffCharReplyMessage : Message
	{
	}

	[Serializable]
	public class EnqueueActionMessage : Message
	{
		public GameAction Action { get; set; }
	}

	[Serializable]
	public class ProceedTickMessage : Message
	{
	}


	[Serializable]
	public class SetTilesMessage : Message
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
	public class CreateBuildingMessage : Message
	{
		public ObjectID MapID { get; set; }
		public IntRect Area { get; set; }
		public int Z { get; set; }
		public BuildingID ID { get; set; }
	}


	[Serializable]
	public class CompoundMessage : Message
	{
		public Message[] Messages { get; set; }

		public override string ToString()
		{
			return String.Format("CompoundMessage");
		}
	}

	[Serializable]
	public abstract class BaseObjectDataMessage : Message
	{
		public ObjectID ObjectID { get; set; }
		public IntPoint3D Location { get; set; }
		public ObjectID Environment { get; set; }

		public Tuple<PropertyID, object>[] Properties { get; set; }
	}

	/* Item in inventory or floor */
	[Serializable]
	public class ItemDataMessage : BaseObjectDataMessage
	{
		public override string ToString()
		{
			return String.Format("ItemDataMessage {0}", this.ObjectID);
		}
	}

	[Serializable]
	public class LivingDataMessage : BaseObjectDataMessage
	{
		public override string ToString()
		{
			return String.Format("LivingDataMessage {0}", this.ObjectID);
		}
	}

	[Serializable]
	public class PropertyDataMessage : Message
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
	public class MapDataMessage : Message
	{
		public ObjectID Environment { get; set; }
		public VisibilityMode VisibilityMode { get; set; }
		public IntCuboid Bounds { get; set; }
	}

	[Serializable]
	public class MapDataTerrainsMessage : Message
	{
		public ObjectID Environment { get; set; }
		public IntCuboid Bounds { get; set; }
		public TileData[] TerrainData { get; set; }
	}

	[Serializable]
	public class MapDataTerrainsListMessage : Message
	{
		public ObjectID Environment { get; set; }
		public Tuple<IntPoint3D, TileData>[] TileDataList { get; set; }

		public override string ToString()
		{
			return String.Format("MapDataTerrainsListMessage({0} tiles)", this.TileDataList.Count());
		}
	}

	[Serializable]
	public class MapDataObjectsMessage : Message
	{
		public ObjectID Environment { get; set; }
		public Message[] ObjectData { get; set; }
	}

	[Serializable]
	public class MapDataBuildingsMessage : Message
	{
		public ObjectID Environment { get; set; }
		public BuildingDataMessage[] BuildingData { get; set; }
	}

	[Serializable]
	public class ObjectMoveMessage : Message
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
	public class ObjectDestructedMessage : Message
	{
		public ObjectID ObjectID { get; set; }

		public override string ToString()
		{
			return String.Format("ObjectDestructedMessage {0}", this.ObjectID);
		}
	}

	[Serializable]
	public class EventMessage : Message
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
	public class BuildingDataMessage : Message
	{
		public ObjectID ObjectID { get; set; }
		public BuildingID ID { get; set; }
		public ObjectID Environment { get; set; }
		public int Z { get; set; }
		public IntRect Area { get; set; }

		public override string ToString()
		{
			return String.Format("BuildingDataMessage");
		}
	}

	[Serializable]
	public class ControllablesDataMessage : Message
	{
		public ObjectID[] Controllables { get; set; }

		public override string ToString()
		{
			return "ControllablesDataMessage";
		}
	}
}
