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
	[DataContract]
	public abstract class Message
	{
	}

	[DataContract]
	public class LogOnRequest : Message
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class LogOnReply : Message
	{
		[DataMember]
		public int UserID { get; set; }
		[DataMember]
		public bool IsSeeAll { get; set; }
	}

	[DataContract]
	public class LogOffRequest : Message
	{
	}

	[DataContract]
	public class LogOffReply : Message
	{
	}

	[DataContract]
	public class LogOnCharRequest : Message
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class LogOnCharReply : Message
	{
	}

	[DataContract]
	public class LogOffCharRequest : Message
	{
	}

	[DataContract]
	public class LogOffCharReply : Message
	{
	}

	[DataContract]
	public class EnqueueActionMessage : Message
	{
		[DataMember]
		public GameAction Action { get; set; }
	}

	[DataContract]
	public class ProceedTickMessage : Message
	{
	}


	[DataContract]
	public class SetTilesMessage : Message
	{
		[DataMember]
		public ObjectID MapID { get; set; }
		[DataMember]
		public IntCuboid Cube { get; set; }
		[DataMember]
		public TileData TileData { get; set; }
	}

	[DataContract]
	public class CompoundMessage : Message
	{
		[DataMember]
		public IEnumerable<Message> Messages { get; set; }

		public override string ToString()
		{
			return String.Format("CompoundMessage");
		}
	}

	/* Item in inventory or floor */
	[DataContract]
	public class ItemData : Message
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public SymbolID SymbolID { get; set; }

		[DataMember]
		public IntPoint3D Location { get; set; }
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public GameColor Color { get; set; }
		[DataMember]
		public MaterialID MaterialID { get; set; }

		public override string ToString()
		{
			return String.Format("ItemData {0} {1}", this.ObjectID, this.Name);
		}
	}

	[DataContract]
	public class LivingData : Message
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public SymbolID SymbolID { get; set; }
		[DataMember]
		public int VisionRange { get; set; }
		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public IntPoint3D Location { get; set; }
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public GameColor Color { get; set; }

		[DataMember]
		public bool Controllable { get; set; }

		public override string ToString()
		{
			return String.Format("LivingData {0} {1}", this.ObjectID, this.Name);
		}
	}

	[DataContract]
	public class MapData : Message
	{
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public VisibilityMode VisibilityMode { get; set; }
		[DataMember]
		public IntCuboid Bounds { get; set; }
	}

	[DataContract]
	public class MapDataTerrains : Message
	{
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public IntCuboid Bounds { get; set; }
		[DataMember]
		public IEnumerable<TileData> TerrainIDs { get; set; }
	}

	[DataContract]
	public class MapDataTerrainsList : Message
	{
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public IEnumerable<KeyValuePair<IntPoint3D, TileData>> TileDataList { get; set; }

		public override string ToString()
		{
			return String.Format("TerrainData({0} tiles)", this.TileDataList.Count());
		}
	}

	[DataContract]
	public class MapDataObjects : Message
	{
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public IEnumerable<Message> ObjectData { get; set; }
	}

	[DataContract]
	public class MapDataBuildings : Message
	{
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public IEnumerable<BuildingData> BuildingData { get; set; }
	}

	[DataContract]
	public class ObjectMove : Message
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public ObjectID TargetEnvID { get; set; }
		[DataMember]
		public IntPoint3D TargetLocation { get; set; }
		[DataMember]
		public ObjectID SourceEnvID { get; set; }
		[DataMember]
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

	[DataContract]
	public class ObjectDestructedMessage : Message
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }

		public override string ToString()
		{
			return String.Format("ObjectDestructedMessage {0}", this.ObjectID);
		}
	}

	[DataContract]
	public class EventMessage : Message
	{
		[DataMember]
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

	[DataContract]
	public class BuildingData : Message
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public BuildingID ID { get; set; }
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public int Z { get; set; }
		[DataMember]
		public IntRect Area { get; set; }

		public override string ToString()
		{
			return String.Format("BuildingData");
		}
	}

	[DataContract]
	public class ControllablesData : Message
	{
		[DataMember]
		public IEnumerable<ObjectID> Controllables { get; set; }

		public override string ToString()
		{
			return "ControllablesData";
		}
	}

}
