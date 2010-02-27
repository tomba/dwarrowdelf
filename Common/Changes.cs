using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	public abstract class Change
	{
		protected Change()
		{
		}
	}

	public class MapChange : Change
	{
		public IIdentifiable Map { get; private set; }
		public ObjectID MapID { get { return this.Map.ObjectID; } }
		public IntPoint3D Location { get; set; }
		public TileData TileData { get; set; }

		public MapChange(IIdentifiable map, IntPoint3D l, TileData tileData)
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
		public IIdentifiable Object { get; private set; }
		public ObjectID ObjectID { get { return this.Object.ObjectID; } }

		protected ObjectChange(IIdentifiable @object)
		{
			this.Object = @object;
		}
	}

	public class ObjectDestructedChange : ObjectChange
	{
		public ObjectDestructedChange(IIdentifiable ob)
			: base(ob)
		{
		}
	}

	public class ObjectMoveChange : ObjectChange
	{
		public IIdentifiable Source { get; private set; }
		public ObjectID SourceMapID
		{
			get
			{
				return this.Source == null ? ObjectID.NullObjectID : this.Source.ObjectID;
			}
		}
		public IntPoint3D SourceLocation { get; private set; }
		public IIdentifiable Destination { get; private set; }
		public ObjectID DestinationMapID
		{
			get
			{
				return this.Destination == null ? ObjectID.NullObjectID : this.Destination.ObjectID;
			}
		}
		public IntPoint3D DestinationLocation { get; private set; }

		public ObjectMoveChange(IIdentifiable mover, IIdentifiable sourceEnv, IntPoint3D sourceLocation,
			IIdentifiable destinationEnv, IntPoint3D destinationLocation)
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

	public class FullObjectChange : ObjectChange
	{
		public FullObjectChange(IIdentifiable ob)
			: base(ob)
		{
		}

		public ClientMsgs.Message ObjectData { get; set; }
	}

}
