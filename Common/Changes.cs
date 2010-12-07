using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	public abstract class Change
	{
	}

	[Serializable]
	public class TickStartChange : Change
	{
		public int TickNumber { get; set; }

		public TickStartChange(int tickNumber)
		{
			this.TickNumber = tickNumber;
		}

		public override string ToString()
		{
			return String.Format("TickStartChange({0})", this.TickNumber);
		}
	}

	[Serializable]
	public class TurnStartChange : Change
	{
		[NonSerialized]
		ILiving m_living;
		ObjectID m_livingID;

		// In sequential mode, the living who's turn it is
		// In simultaneous mode, null
		public ILiving Living { get { return m_living; } }
		public ObjectID LivingID { get { return m_livingID; } }

		public TurnStartChange(ILiving living = null)
		{
			m_living = living;
			if (living != null)
				m_livingID = living.ObjectID;
		}
	}

	[Serializable]
	public class TurnEndChange : Change
	{
		[NonSerialized]
		ILiving m_living;
		ObjectID m_livingID;

		public ILiving Living { get { return m_living; } }
		public ObjectID LivingID { get { return m_livingID; } }

		public TurnEndChange(ILiving living = null)
		{
			m_living = living;
			if (living != null)
				m_livingID = living.ObjectID;
		}
	}

	[Serializable]
	public abstract class EnvironmentChange : Change
	{
		[NonSerialized]
		IEnvironment m_environment;
		ObjectID m_environmentID;

		public IEnvironment Environment { get { return m_environment; } }
		public ObjectID EnvironmentID { get { return m_environmentID; } }

		public EnvironmentChange(IEnvironment env)
		{
			m_environment = env;
			m_environmentID = env.ObjectID;
		}
	}

	[Serializable]
	public class MapChange : EnvironmentChange
	{
		IntPoint3D m_location;
		TileData m_tileData;

		public IntPoint3D Location { get { return m_location; } }
		public TileData TileData { get { return m_tileData; } }

		public MapChange(IEnvironment map, IntPoint3D l, TileData tileData)
			: base(map)
		{
			m_location = l;
			m_tileData = tileData;
		}

		public override string ToString()
		{
			return String.Format("MapChange {0}, {1}, {2}", this.EnvironmentID, this.Location, this.TileData);
		}
	}

	[Serializable]
	public abstract class ObjectChange : Change
	{
		[NonSerialized]
		IBaseGameObject m_object;
		ObjectID m_objectID;

		public IBaseGameObject Object { get { return m_object; } }
		public ObjectID ObjectID { get { return m_objectID; } }

		protected ObjectChange(IBaseGameObject @object)
		{
			m_object = @object;
			m_objectID = m_object.ObjectID;
		}
	}

	[Serializable]
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

	[Serializable]
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

	[Serializable]
	public class ObjectMoveChange : ObjectChange
	{
		[NonSerialized]
		IGameObject m_source;
		ObjectID m_sourceID;
		IntPoint3D m_sourceLocation;
		[NonSerialized]
		IGameObject m_destination;
		ObjectID m_destinationID;
		IntPoint3D m_destinationLocation;

		public IGameObject Source { get { return m_source; } }
		public ObjectID SourceMapID { get { return m_sourceID; } }
		public IntPoint3D SourceLocation { get { return m_sourceLocation; } }

		public IGameObject Destination { get { return m_destination; } }
		public ObjectID DestinationMapID { get { return m_destinationID; } }
		public IntPoint3D DestinationLocation { get { return m_destinationLocation; } }

		public ObjectMoveChange(IGameObject mover, IGameObject sourceEnv, IntPoint3D sourceLocation,
			IGameObject destinationEnv, IntPoint3D destinationLocation)
			: base(mover)
		{
			m_source = sourceEnv;
			if (m_source != null)
				m_sourceID = m_source.ObjectID;
			m_sourceLocation = sourceLocation;
			m_destination = destinationEnv;
			if (m_destination != null)
				m_destinationID = m_destination.ObjectID;
			m_destinationLocation = destinationLocation;
		}

		public override string ToString()
		{
			return String.Format("ObjectMoveChange {0} ({1}, {2}) -> ({3}, {4})",
				this.ObjectID, this.SourceMapID, this.SourceLocation, this.DestinationMapID, this.DestinationLocation);
		}
	}

	[Serializable]
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

	[Serializable]
	public class PropertyChange : ObjectChange
	{
		[NonSerialized]
		PropertyDefinition m_property;
		PropertyID m_propertyID;
		object m_value;

		public PropertyDefinition Property { get { return m_property; } }
		public PropertyID PropertyID { get { return m_propertyID; } }
		public object Value { get { return m_value; } }

		public PropertyChange(IBaseGameObject ob, PropertyDefinition property, object value)
			: base(ob)
		{
			m_property = property;
			m_propertyID = property.PropertyID;
			m_value = value;
		}

		public override string ToString()
		{
			return String.Format("PropertyChange({0}, {1} : {2})", this.ObjectID, this.PropertyID, this.Value);
		}
	}

	[Serializable]
	public class ActionStartedChange : ObjectChange
	{
		public GameAction Action { get; set; }
		public int TicksLeft { get; set; }
		public int UserID { get; set; }

		public ActionStartedChange(IBaseGameObject ob)
			: base(ob)
		{
		}

		public override string ToString()
		{
			return String.Format("ActionStartedChange({0}, {1}, left: {2}, uid: {3})",
				this.ObjectID, this.Action, this.TicksLeft, this.UserID);
		}
	}

	[Serializable]
	public class ActionProgressChange : ObjectChange
	{
		// just for debug
		public GameAction ActionXXX { get; set; }
		public int UserID { get; set; }
		public int TicksLeft { get; set; }
		public ActionState State { get; set; }

		public ActionProgressChange(IBaseGameObject ob)
			: base(ob)
		{
		}

		public override string ToString()
		{
			return String.Format("ActionProgressChange(UID({0}), {1}, left: {2}, state: {3})",
				this.UserID, this.ObjectID, this.TicksLeft, this.State);
		}
	}
}
