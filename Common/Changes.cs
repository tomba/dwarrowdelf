using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	public abstract class Change
	{
	}

	public class MapChange : Change
	{
		public IEnvironment Map { get; private set; }
		public ObjectID MapID { get { return this.Map.ObjectID; } }
		public IntPoint3D Location { get; set; }
		public TileData TileData { get; set; }

		public MapChange(IEnvironment map, IntPoint3D l, TileData tileData)
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
		public IBaseGameObject Object { get; private set; }
		public ObjectID ObjectID { get { return this.Object.ObjectID; } }

		protected ObjectChange(IBaseGameObject @object)
		{
			this.Object = @object;
		}
	}

	public class ObjectCreatedChange : ObjectChange
	{
		public ObjectCreatedChange(IBaseGameObject ob)
			: base(ob)
		{
		}

		public override string ToString()
		{
			return String.Format("ObjectCreatedChange({0})", this.ObjectID);
		}
	}

	public class ObjectDestructedChange : ObjectChange
	{
		public ObjectDestructedChange(IBaseGameObject ob)
			: base(ob)
		{
		}

		public override string ToString()
		{
			return String.Format("ObjectDestructedChange({0})", this.ObjectID);
		}
	}

	public class ObjectMoveChange : ObjectChange
	{
		public IGameObject Source { get; private set; }
		public ObjectID SourceMapID
		{
			get
			{
				return this.Source == null ? ObjectID.NullObjectID : this.Source.ObjectID;
			}
		}
		public IntPoint3D SourceLocation { get; private set; }
		public IGameObject Destination { get; private set; }
		public ObjectID DestinationMapID
		{
			get
			{
				return this.Destination == null ? ObjectID.NullObjectID : this.Destination.ObjectID;
			}
		}
		public IntPoint3D DestinationLocation { get; private set; }

		public ObjectMoveChange(IGameObject mover, IGameObject sourceEnv, IntPoint3D sourceLocation,
			IGameObject destinationEnv, IntPoint3D destinationLocation)
			: base(mover)
		{
			this.Source = sourceEnv;
			this.SourceLocation = sourceLocation;
			this.Destination = destinationEnv;
			this.DestinationLocation = destinationLocation;
		}

		public override string ToString()
		{
			return String.Format("ObjectMoveChange {0} ({1}, {2}) -> ({3}, {4})",
				this.ObjectID, this.SourceMapID, this.SourceLocation, this.DestinationMapID, this.DestinationLocation);
		}
	}

	public class FullObjectChange : ObjectChange
	{
		public FullObjectChange(IBaseGameObject ob)
			: base(ob)
		{
		}

		public BaseGameObjectData ObjectData { get; set; }

		public override string ToString()
		{
			return String.Format("FullObjectChange({0})", this.ObjectID);
		}
	}

	public class PropertyChange : ObjectChange
	{
		public PropertyChange(IBaseGameObject ob, PropertyDefinition property, object value)
			: base(ob)
		{
			this.Property = property;
			this.Value = value;
		}

		public PropertyDefinition Property { get; private set; }
		public PropertyID PropertyID { get { return Property.PropertyID; } }
		public object Value { get; private set; }

		public override string ToString()
		{
			return String.Format("PropertyChange({0}, {1} : {2})", this.ObjectID, this.PropertyID, this.Value);
		}
	}
}
