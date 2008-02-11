using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract, 
	KnownType(typeof(TurnChange)),
	KnownType(typeof(MapChange)),
	KnownType(typeof(ObjectChange)),
	KnownType(typeof(ObjectLocationChange)), 
	KnownType(typeof(ObjectEnvironmentChange))
	]
	public abstract class Change
	{
	}

	[DataContract]
	public class TurnChange : Change
	{
		[DataMember]
		public int TurnNumber { get; set; }

		public TurnChange(int turnNumber)
		{
			this.TurnNumber = turnNumber;
		}

		public override string ToString()
		{
			return String.Format("TurnChange({0})", this.TurnNumber);
		}
	}

	[DataContract]
	public class MapChange : Change
	{
		[DataMember]
		public ObjectID MapID { get; set; }
		[DataMember]
		public Location Location { get; set; }
		[DataMember]
		public int TerrainType { get; set; }

		public MapChange(ObjectID mapID, Location l, int terrainType)
		{
			this.MapID = mapID;
			this.Location = l;
			this.TerrainType = terrainType;
		}

		public override string ToString()
		{
			return String.Format("MapChange {0}, {1}, {2}", this.MapID, this.Location, this.TerrainType);
		}
	}

	[DataContract]
	public abstract class ObjectChange : Change
	{
		GameObject m_target;
		[DataMember]
		public ObjectID ObjectID { get; set; }

		public ObjectChange(GameObject target)
		{
			m_target = target;
			this.ObjectID = m_target.ObjectID;
		}
	}

	[DataContract]
	public class ObjectLocationChange : ObjectChange
	{
		[DataMember]
		public Location TargetLocation { get; set; }
		[DataMember]
		public Location SourceLocation { get; set; }

		public ObjectLocationChange(GameObject target, Location from, Location to)
			: base(target)
		{
			this.SourceLocation = from;
			this.TargetLocation = to;
		}

		public override string ToString()
		{
			return String.Format("LocationChange {0} {1}->{2}", this.ObjectID, 
				this.SourceLocation, this.TargetLocation);
		}
	}

	[DataContract]
	public class ObjectEnvironmentChange : ObjectChange
	{
		[DataMember]
		public Location Location { get; set; }
		[DataMember]
		public ObjectID MapID { get; set; }

		public ObjectEnvironmentChange(GameObject target, ObjectID mapID, Location location)
			: base(target)
		{
			this.Location = location;
			this.MapID = mapID;
		}

		public override string ToString()
		{
			return String.Format("EnvironmentChange {0}, {1}, {2}", this.ObjectID, this.MapID, this.Location);
		}
	}
}
