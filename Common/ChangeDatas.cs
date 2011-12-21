using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	public abstract class ChangeData
	{
	}

	[Serializable]
	public class TickStartChangeData : ChangeData
	{
		public int TickNumber;
	}

	[Serializable]
	public class TurnStartSimultaneousChangeData : ChangeData
	{
	}

	[Serializable]
	public class TurnEndSimultaneousChangeData : ChangeData
	{
	}

	[Serializable]
	public class TurnStartSequentialChangeData : ChangeData
	{
		public ObjectID LivingID;
	}

	[Serializable]
	public class TurnEndSequentialChangeData : ChangeData
	{
		public ObjectID LivingID;
	}

	[Serializable]
	public abstract class EnvironmentChangeData : ChangeData
	{
		public ObjectID EnvironmentID;
	}

	[Serializable]
	public class MapChangeData : EnvironmentChangeData
	{
		public IntPoint3D Location;
		public TileData TileData;
	}

	[Serializable]
	public abstract class ObjectChangeData : ChangeData
	{
		public ObjectID ObjectID;
	}

	[Serializable]
	public class ObjectCreatedChangeData : ObjectChangeData
	{
	}

	[Serializable]
	public class ObjectDestructedChangeData : ObjectChangeData
	{
	}

	[Serializable]
	public class FullObjectChangeData : ObjectChangeData
	{
		public BaseGameObjectData ObjectData;
	}

	[Serializable]
	public abstract class PropertyChangeData : ObjectChangeData
	{
		public PropertyID PropertyID;
	}

	[Serializable]
	public class PropertyValueChangeData : PropertyChangeData
	{
		public ValueType Value;
	}

	[Serializable]
	public class PropertyIntChangeData : PropertyChangeData
	{
		public int Value;
	}

	[Serializable]
	public class PropertyStringChangeData : PropertyChangeData
	{
		public string Value;
	}

	[Serializable]
	public class ObjectMoveChangeData : ObjectChangeData
	{
		public ObjectID SourceID;
		public IntPoint3D SourceLocation;

		public ObjectID DestinationID;
		public IntPoint3D DestinationLocation;
	}

	[Serializable]
	public class ObjectMoveLocationChangeData : ObjectChangeData
	{
		public IntPoint3D SourceLocation;
		public IntPoint3D DestinationLocation;
	}



	[Serializable]
	public class SkillChangeData : ObjectChangeData
	{
		public SkillID SkillID;
		public byte Level;
	}

	[Serializable]
	public class ActionStartedChangeData : ObjectChangeData
	{
		public ActionStartEvent ActionStartEvent;
	}

	[Serializable]
	public class ActionProgressChangeData : ObjectChangeData
	{
		public ActionProgressEvent ActionProgressEvent;
	}

	[Serializable]
	public class ActionDoneChangeData : ObjectChangeData
	{
		public ActionDoneEvent ActionDoneEvent;
	}

	[Serializable]
	public class WearChangeData : ObjectChangeData
	{
		public ObjectID WearableID;
		public ArmorSlot Slot;
	}

	[Serializable]
	public class WieldChangeData : ObjectChangeData
	{
		public ObjectID WeaponID;
	}
}
