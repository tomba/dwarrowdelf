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
	public abstract class Message
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

		public override string ToString()
		{
			return String.Format("LivingData {0} {1}", this.ObjectID, this.Name);
		}
	}

	/* Tile that came visible */
	[DataContract]
	public class MapData
	{
		[DataMember]
		public Location Location { get; set; }
		[DataMember]
		public int Terrain { get; set; }
		[DataMember]
		public ObjectID[] Objects { get; set; }
	}

	[DataContract]
	public class TerrainData : Message
	{
		[DataMember]
		public MapData[] MapDataList { get; set; }

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
		public Location TargetLocation { get; set; }
		[DataMember]
		public Location SourceLocation { get; set; }
		[DataMember]
		public int Symbol { get; set; }

		public ObjectMove(GameObject target, int symbol, Location from, Location to)
		{
			this.ObjectID = target.ObjectID;
			this.Symbol = symbol;
			this.SourceLocation = from;
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
