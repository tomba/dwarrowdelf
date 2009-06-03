using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace MyGame
{
	public abstract class Change
	{
	}

	public class TurnChange : Change
	{
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

	public class MapChange : Change
	{
		public ObjectID MapID { get; set; }
		public Location Location { get; set; }
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

	public abstract class ObjectChange : Change
	{
		public GameObject Target { get; set; }
		public ObjectID ObjectID { get; set; }

		public ObjectChange(GameObject target)
		{
			this.Target = target;
			this.ObjectID = target.ObjectID;
		}
	}

	public class ObjectLocationChange : ObjectChange
	{
		public Location TargetLocation { get; set; }
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

	public class ObjectEnvironmentChange : ObjectChange
	{
		public ObjectID SourceMapID { get; set; }
		public Location SourceLocation { get; set; }
		public ObjectID DestinationMapID { get; set; }
		public Location DestinationLocation { get; set; }

		public ObjectEnvironmentChange(GameObject target, ObjectID sourceMapID, Location sourceLocation,
			ObjectID destinationMapID, Location destinationLocation)
			: base(target)
		{
			this.SourceMapID = sourceMapID;
			this.SourceLocation = sourceLocation;
			this.DestinationMapID = destinationMapID;
			this.DestinationLocation = destinationLocation;
		}

		public override string ToString()
		{
			return String.Format("EnvironmentChange {0} ({1}, {2}) -> ({3}, {4})",
				this.ObjectID, this.SourceMapID, this.SourceLocation, this.DestinationMapID, this.DestinationLocation);
		}
	}
}
