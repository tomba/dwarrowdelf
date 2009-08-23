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
	KnownType(typeof(ItemData)),
	KnownType(typeof(ItemsData)),
	KnownType(typeof(LivingData)),
	KnownType(typeof(MapData)),
	KnownType(typeof(TerrainData)),
	KnownType(typeof(ObjectMove)),
	KnownType(typeof(TurnChange))]
	public class Message
	{
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
		public IntPoint Location { get; set; }
		[DataMember]
		public ObjectID Environment { get; set; }

		public override string ToString()
		{
			return String.Format("ItemData {0} {1}", this.ObjectID, this.Name);
		}
	}

	[DataContract]
	public class ItemsData : Message
	{
		[DataMember]
		public ItemData[] Items { get; set; }
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
		public IntPoint Location { get; set; }
		[DataMember]
		public ObjectID Environment { get; set; }

		public override string ToString()
		{
			return String.Format("LivingData {0} {1}", this.ObjectID, this.Name);
		}
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
	public struct MapTileData
	{
		public IntPoint Location { get; set; }
		public int TerrainID { get; set; }
	}

	[DataContract]
	public class TerrainData : Message
	{
		[DataMember]
		public ObjectID Environment { get; set; }
		[DataMember]
		public MapTileData[] MapDataList { get; set; }

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
		public IntPoint TargetLocation { get; set; }
		[DataMember]
		public ObjectID SourceEnvID { get; set; }
		[DataMember]
		public IntPoint SourceLocation { get; set; }

		public ObjectMove(GameObject target, ObjectID fromID, IntPoint from, ObjectID toID, IntPoint to)
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
	public class TurnChange : Message
	{
		[DataMember]
		public int TurnNumber { get; set; }

		public override string ToString()
		{
			return String.Format("TurnChange({0})", this.TurnNumber);
		}
	}



}
