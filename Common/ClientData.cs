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
	[DataContract,
	KnownType(typeof(CompoundMessage)),
	KnownType(typeof(ItemData)),
	KnownType(typeof(LivingData)),
	KnownType(typeof(FullMapData)),
	KnownType(typeof(MapData)),
	KnownType(typeof(MapTileData)),
	KnownType(typeof(TerrainData)),
	KnownType(typeof(ObjectMove)),
	KnownType(typeof(EventMessage)),
	]
	public abstract class Message
	{
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
		public int SymbolID { get; set; }

		[DataMember]
		public IntPoint3D Location { get; set; }
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public GameColor Color { get; set; }

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
		public int SymbolID { get; set; }
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
	public class FullMapData : Message
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public VisibilityMode VisibilityMode { get; set; }
		[DataMember]
		public IntCube Bounds { get; set; }
		[DataMember]
		public IEnumerable<int> TerrainIDs { get; set; }
		[DataMember]
		public IEnumerable<ClientMsgs.Message> ObjectData { get; set; }
	}


	[DataContract]
	public class MapData : Message
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public VisibilityMode VisibilityMode { get; set; }
	}

	/* Tile that came visible */
	[DataContract]
	public struct MapTileData
	{
		[DataMember]
		public IntPoint3D Location { get; set; }
		[DataMember]
		public int TerrainID { get; set; }
	}

	[DataContract]
	public class TerrainData : Message
	{
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public IEnumerable<MapTileData> MapDataList { get; set; }

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("TerrainData ");

			string[] arr = this.MapDataList.
				Select(md => String.Format("{0},{1}", md.Location.X, md.Location.Y)).
				ToArray();

			sb.Append(String.Join(" ", arr));

			return sb.ToString();
		}
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

		public ObjectMove(GameObject target, ObjectID fromID, IntPoint3D from, ObjectID toID, IntPoint3D to)
		{
			this.ObjectID = target.ObjectID;
			this.SourceEnvID = fromID;
			this.SourceLocation = from;
			this.TargetEnvID = toID;
			this.TargetLocation = to;
		}

		public override string ToString()
		{
			return String.Format("ObjectMove {0} {1}->{2}", this.ObjectID,
				this.SourceLocation, this.TargetLocation);
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


}
