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
	public sealed class TickStartChangeData : ChangeData
	{
		public int TickNumber;
	}

	[Serializable]
	public sealed class TurnStartSimultaneousChangeData : ChangeData
	{
	}

	[Serializable]
	public sealed class TurnEndSimultaneousChangeData : ChangeData
	{
	}

	[Serializable]
	public sealed class TurnStartSequentialChangeData : ChangeData
	{
		public ObjectID LivingID;
	}

	[Serializable]
	public sealed class TurnEndSequentialChangeData : ChangeData
	{
		public ObjectID LivingID;
	}

	[Serializable]
	public abstract class EnvironmentChangeData : ChangeData
	{
		public ObjectID EnvironmentID;
	}

	[Serializable]
	public sealed class MapChangeData : EnvironmentChangeData
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
	public sealed class ObjectCreatedChangeData : ObjectChangeData
	{
	}

	[Serializable]
	public sealed class ObjectDestructedChangeData : ObjectChangeData
	{
	}

	[Serializable]
	public sealed class FullObjectChangeData : ObjectChangeData
	{
		public BaseGameObjectData ObjectData;
	}

	[Serializable]
	public abstract class PropertyChangeData : ObjectChangeData
	{
		public PropertyID PropertyID;
	}

	[Serializable]
	public sealed class PropertyValueChangeData : PropertyChangeData
	{
		public ValueType Value;
	}

	[Serializable]
	public sealed class PropertyIntChangeData : PropertyChangeData
	{
		public int Value;
	}

	[Serializable]
	public sealed class PropertyStringChangeData : PropertyChangeData
	{
		public string Value;
	}

	[Serializable]
	public sealed class ObjectMoveChangeData : ObjectChangeData
	{
		public ObjectID SourceID;
		public IntPoint3D SourceLocation;

		public ObjectID DestinationID;
		public IntPoint3D DestinationLocation;
	}

	[Serializable]
	public sealed class ObjectMoveLocationChangeData : ObjectChangeData
	{
		public IntPoint3D SourceLocation;
		public IntPoint3D DestinationLocation;
	}



	[Serializable]
	public sealed class SkillChangeData : ObjectChangeData
	{
		public SkillID SkillID;
		public byte Level;
	}

	[Serializable]
	public sealed class ActionStartedChangeData : ObjectChangeData
	{
		public ActionStartEvent ActionStartEvent;
	}

	[Serializable]
	public sealed class ActionProgressChangeData : ObjectChangeData
	{
		public ActionProgressEvent ActionProgressEvent;
	}

	[Serializable]
	public sealed class ActionDoneChangeData : ObjectChangeData
	{
		public ActionDoneEvent ActionDoneEvent;
	}

	[Serializable]
	public sealed class WearChangeData : ObjectChangeData
	{
		public ObjectID WearableID;
		public ArmorSlot Slot;
	}

	[Serializable]
	public sealed class WieldChangeData : ObjectChangeData
	{
		public ObjectID WeaponID;
	}
}
