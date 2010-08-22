using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

/*
 * Classes to deliver data to client
 */

namespace MyGame.ClientMsgs
{
	[Serializable]
	public abstract class Message
	{
	}

	[Serializable]
	public class IronPythonCommand : Message
	{
		public string Text { get; set; }
	}

	[Serializable]
	public class IronPythonOutput : Message
	{
		public string Text { get; set; }
	}

	[Serializable]
	public class LogOnRequest : Message
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class LogOnReply : Message
	{
		public int UserID { get; set; }
		public bool IsSeeAll { get; set; }
	}

	[Serializable]
	public class LogOffRequest : Message
	{
	}

	[Serializable]
	public class LogOffReply : Message
	{
	}

	[Serializable]
	public class LogOnCharRequest : Message
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class LogOnCharReply : Message
	{
	}

	[Serializable]
	public class LogOffCharRequest : Message
	{
	}

	[Serializable]
	public class LogOffCharReply : Message
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
	public abstract class BaseObjectData : Message
	{
		public ObjectID ObjectID { get; set; }
		public IntPoint3D Location { get; set; }
		public ObjectID Environment { get; set; }

		public Tuple<PropertyID, object>[] Properties { get; set; }
	}

	/* Item in inventory or floor */
	[Serializable]
	public class ItemData : BaseObjectData
	{
		public override string ToString()
		{
			return String.Format("ItemData {0}", this.ObjectID);
		}
	}

	[Serializable]
	public class LivingData : BaseObjectData
	{
		public override string ToString()
		{
			return String.Format("LivingData {0}", this.ObjectID);
		}
	}

	[Serializable]
	public class PropertyData : Message
	{
		public ObjectID ObjectID { get; set; }
		public PropertyID PropertyID { get; set; }
		public object Value { get; set; }

		public override string ToString()
		{
			return String.Format("PropertyData {0} {1}: {2}", this.ObjectID, this.PropertyID, this.Value);
		}
	}

	[Serializable]
	public class MapData : Message
	{
		public ObjectID Environment { get; set; }
		public VisibilityMode VisibilityMode { get; set; }
		public IntCuboid Bounds { get; set; }
	}

	[Serializable]
	public class MapDataTerrains : Message
	{
		public ObjectID Environment { get; set; }
		public IntCuboid Bounds { get; set; }
		public TileData[] TerrainData { get; set; }
	}

	[Serializable]
	public class MapDataTerrainsList : Message
	{
		public ObjectID Environment { get; set; }
		public Tuple<IntPoint3D, TileData>[] TileDataList { get; set; }

		public override string ToString()
		{
			return String.Format("TerrainData({0} tiles)", this.TileDataList.Count());
		}
	}

	[Serializable]
	public class MapDataObjects : Message
	{
		public ObjectID Environment { get; set; }
		public Message[] ObjectData { get; set; }
	}

	[Serializable]
	public class MapDataBuildings : Message
	{
		public ObjectID Environment { get; set; }
		public BuildingData[] BuildingData { get; set; }
	}

	[Serializable]
	public class ObjectMove : Message
	{
		public ObjectID ObjectID { get; set; }
		public ObjectID TargetEnvID { get; set; }
		public IntPoint3D TargetLocation { get; set; }
		public ObjectID SourceEnvID { get; set; }
		public IntPoint3D SourceLocation { get; set; }

		public ObjectMove(IIdentifiable target, ObjectID fromID, IntPoint3D from, ObjectID toID, IntPoint3D to)
		{
			this.ObjectID = target.ObjectID;
			this.SourceEnvID = fromID;
			this.SourceLocation = from;
			this.TargetEnvID = toID;
			this.TargetLocation = to;
		}

		public override string ToString()
		{
			return String.Format("ObjectMove {0} {1}/{2}->{3}/{4}", this.ObjectID,
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
	public class BuildingData : Message
	{
		public ObjectID ObjectID { get; set; }
		public BuildingID ID { get; set; }
		public ObjectID Environment { get; set; }
		public int Z { get; set; }
		public IntRect Area { get; set; }

		public override string ToString()
		{
			return String.Format("BuildingData");
		}
	}

	[Serializable]
	public class ControllablesData : Message
	{
		public ObjectID[] Controllables { get; set; }

		public override string ToString()
		{
			return "ControllablesData";
		}
	}
}
