using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract,
	KnownType(typeof(TurnChangeEvent)),
	KnownType(typeof(ActionProgressEvent)),
	KnownType(typeof(ActionRequiredEvent)),
	]
	public abstract class Event
	{
	}

	[DataContract]
	public class TurnChangeEvent : Event
	{
		[DataMember]
		public int TurnNumber { get; set; }

		public TurnChangeEvent(int turnNumber)
		{
			this.TurnNumber = turnNumber;
		}

		public override string ToString()
		{
			return String.Format("TurnChangeEvent({0})", this.TurnNumber);
		}
	}

	[DataContract]
	public class ActionProgressEvent : Event
	{
		public int UserID { get; set; }
		[DataMember]
		public int TransactionID { get; set; }
		[DataMember]
		public int TurnsLeft { get; set; }
		[DataMember]
		public bool Success { get; set; }

		public override string ToString()
		{
			return String.Format("ActionProgressEvent({0}, trid: {1}, left: {2}, ok: {3})",
				this.UserID, this.TransactionID, this.TurnsLeft, this.Success);
		}
	}

	[DataContract]
	public class ActionRequiredEvent : Event
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }

		public override string ToString()
		{
			return String.Format("ActionRequiredEvent({0})", this.ObjectID);
		}
	}



	public abstract class Change
	{
		protected Change()
		{
		}
	}

	public class MapChange : Change
	{
		public GameObject Map { get; private set; }
		public ObjectID MapID { get { return this.Map.ObjectID; } }
		public IntPoint3D Location { get; set; }
		public TileData TileData { get; set; }

		public MapChange(GameObject map, IntPoint3D l, TileData tileData)
		{
			this.Map = map;
			this.Location = l;
			this.TileData = tileData;
		}

		public override string ToString()
		{
			return String.Format("MapChange {0}, {1}, {2}", this.MapID, this.Location, this.TileData);
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
