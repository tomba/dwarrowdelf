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
		protected Change()
		{
		}
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
		public GameObject Map { get; private set; }
		public ObjectID MapID { get { return this.Map.ObjectID; } }
		public IntPoint3D Location { get; set; }
		public int TerrainType { get; set; }

		public MapChange(GameObject map, IntPoint3D l, int terrainType)
		{
			this.Map = map;
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
		public GameObject Object { get; private set; }
		public ObjectID ObjectID { get { return this.Object.ObjectID; } }

		public ObjectChange(GameObject @object)
		{
			this.Object = @object;
		}
	}

	public class ObjectMoveChange : ObjectChange
	{
		public GameObject Source { get; private set; }
		public ObjectID SourceMapID
		{
			get
			{
				return this.Source == null ? ObjectID.NullObjectID : this.Source.ObjectID;
			}
		}
		public IntPoint3D SourceLocation { get; private set; }
		public GameObject Destination { get; private set; }
		public ObjectID DestinationMapID
		{
			get
			{
				return this.Destination == null ? ObjectID.NullObjectID : this.Destination.ObjectID;
			}
		}
		public IntPoint3D DestinationLocation { get; private set; }

		public ObjectMoveChange(GameObject mover, GameObject sourceEnv, IntPoint3D sourceLocation,
			GameObject destinationEnv, IntPoint3D destinationLocation)
			: base(mover)
		{
			this.Source = sourceEnv;
			this.SourceLocation = sourceLocation;
			this.Destination = destinationEnv;
			this.DestinationLocation = destinationLocation;
		}

		public override string ToString()
		{
			return String.Format("MoveChange {0} ({1}, {2}) -> ({3}, {4})",
				this.ObjectID, this.SourceMapID, this.SourceLocation, this.DestinationMapID, this.DestinationLocation);
		}
	}
}
