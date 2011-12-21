using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf.Server
{
	public abstract class Change
	{
		public abstract ChangeData ToChangeData();
	}

	public class TickStartChange : Change
	{
		public int TickNumber { get; private set; }

		public TickStartChange(int tickNumber)
		{
			this.TickNumber = tickNumber;
		}

		public override ChangeData ToChangeData()
		{
			return new TickStartChangeData() { TickNumber = this.TickNumber };
		}
	}

	public class TurnStartSimultaneousChange : Change
	{
		public TurnStartSimultaneousChange()
		{
		}

		public override ChangeData ToChangeData()
		{
			return new TurnStartSimultaneousChangeData();
		}
	}

	public class TurnEndSimultaneousChange : Change
	{
		public TurnEndSimultaneousChange()
		{
		}

		public override ChangeData ToChangeData()
		{
			return new TurnEndSimultaneousChangeData();
		}
	}

	public class TurnStartSequentialChange : Change
	{
		public ILivingObject Living { get; private set; }

		public TurnStartSequentialChange(ILivingObject living)
		{
			this.Living = living;
		}

		public override ChangeData ToChangeData()
		{
			return new TurnStartSequentialChangeData() { LivingID = ObjectID.GetID(this.Living) };
		}
	}

	public class TurnEndSequentialChange : Change
	{
		public ILivingObject Living { get; private set; }

		public TurnEndSequentialChange(ILivingObject living)
		{
			this.Living = living;
		}

		public override ChangeData ToChangeData()
		{
			return new TurnEndSequentialChangeData() { LivingID = ObjectID.GetID(this.Living) };
		}
	}

	public abstract class EnvironmentChange : Change
	{
		public IEnvironmentObject Environment { get; private set; }

		public EnvironmentChange(IEnvironmentObject env)
		{
			this.Environment = env;
		}
	}

	public class MapChange : EnvironmentChange
	{
		public IntPoint3D Location { get; private set; }
		public TileData TileData { get; private set; }

		public MapChange(IEnvironmentObject map, IntPoint3D l, TileData tileData)
			: base(map)
		{
			this.Location = l;
			this.TileData = tileData;
		}

		public override ChangeData ToChangeData()
		{
			return new MapChangeData() { EnvironmentID = ObjectID.GetID(this.Environment), Location = this.Location, TileData = this.TileData };
		}
	}

	public abstract class ObjectChange : Change
	{
		public IBaseObject Object { get; private set; }

		protected ObjectChange(IBaseObject ob)
		{
			this.Object = ob;
		}
	}

	public class ObjectCreatedChange : ObjectChange
	{
		public ObjectCreatedChange(IBaseObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ObjectCreatedChangeData() { ObjectID = ObjectID.GetID(this.Object) };
		}
	}

	public class ObjectDestructedChange : ObjectChange
	{
		public ObjectDestructedChange(IBaseObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ObjectDestructedChangeData() { ObjectID = ObjectID.GetID(this.Object) };
		}
	}

	public class FullObjectChange : ObjectChange
	{
		public BaseGameObjectData ObjectData { get; set; }

		public FullObjectChange(IBaseObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new FullObjectChangeData() { ObjectID = ObjectID.GetID(this.Object), ObjectData = this.ObjectData };
		}
	}

	public abstract class PropertyChange : ObjectChange
	{
		public PropertyID PropertyID { get; private set; }

		protected PropertyChange(IBaseObject ob, PropertyID propertyID)
			: base(ob)
		{
			this.PropertyID = propertyID;
		}
	}

	public class PropertyValueChange : PropertyChange
	{
		public ValueType Value { get; private set; }

		public PropertyValueChange(IBaseObject ob, PropertyID propertyID, ValueType value)
			: base(ob, propertyID)
		{
			this.Value = value;
		}

		public override ChangeData ToChangeData()
		{
			return new PropertyValueChangeData() { ObjectID = ObjectID.GetID(this.Object), PropertyID = this.PropertyID, Value = this.Value };
		}
	}

	public class PropertyIntChange : PropertyChange
	{
		public int Value { get; private set; }

		public PropertyIntChange(IBaseObject ob, PropertyID propertyID, int value)
			: base(ob, propertyID)
		{
			this.Value = value;
		}

		public override ChangeData ToChangeData()
		{
			return new PropertyIntChangeData() { ObjectID = ObjectID.GetID(this.Object), PropertyID = this.PropertyID, Value = this.Value };
		}
	}

	public class PropertyStringChange : PropertyChange
	{
		public string Value { get; private set; }

		public PropertyStringChange(IBaseObject ob, PropertyID propertyID, string value)
			: base(ob, propertyID)
		{
			this.Value = value;
		}

		public override ChangeData ToChangeData()
		{
			return new PropertyStringChangeData() { ObjectID = ObjectID.GetID(this.Object), PropertyID = this.PropertyID, Value = this.Value };
		}
	}

	public class ObjectMoveChange : ObjectChange
	{
		public IContainerObject Source { get; private set; }
		public IntPoint3D SourceLocation { get; private set; }

		public IContainerObject Destination { get; private set; }
		public IntPoint3D DestinationLocation { get; private set; }

		public ObjectMoveChange(IMovableObject mover, IContainerObject sourceEnv, IntPoint3D sourceLocation,
			IContainerObject destinationEnv, IntPoint3D destinationLocation)
			: base(mover)
		{
			this.Source = sourceEnv;
			this.SourceLocation = sourceLocation;
			this.Destination = destinationEnv;
			this.DestinationLocation = destinationLocation;
		}

		public override ChangeData ToChangeData()
		{
			return new ObjectMoveChangeData()
			{
				ObjectID = ObjectID.GetID(this.Object),
				SourceID = ObjectID.GetID(this.Source),
				SourceLocation = this.SourceLocation,
				DestinationID = ObjectID.GetID(this.Destination),
				DestinationLocation = this.DestinationLocation
			};
		}
	}

	public class ObjectMoveLocationChange : ObjectChange
	{
		public IntPoint3D SourceLocation { get; private set; }
		public IntPoint3D DestinationLocation { get; private set; }

		public ObjectMoveLocationChange(IMovableObject mover, IntPoint3D sourceLocation, IntPoint3D destinationLocation)
			: base(mover)
		{
			this.SourceLocation = sourceLocation;
			this.DestinationLocation = destinationLocation;
		}
		public override ChangeData ToChangeData()
		{
			return new ObjectMoveLocationChangeData()
			{
				ObjectID = ObjectID.GetID(this.Object),
				SourceLocation = this.SourceLocation,
				DestinationLocation = this.DestinationLocation
			};
		}
	}



	public class SkillChange : ObjectChange
	{
		public SkillID SkillID { get; private set; }
		public byte Level { get; private set; }

		public SkillChange(ILivingObject ob, SkillID skillID, byte level)
			: base(ob)
		{
			this.SkillID = skillID;
			this.Level = level;
		}

		public override ChangeData ToChangeData()
		{
			return new SkillChangeData() { ObjectID = ObjectID.GetID(this.Object), SkillID = this.SkillID, Level = this.Level };
		}
	}

	public class ActionStartedChange : ObjectChange
	{
		public ActionStartEvent ActionStartEvent { get; set; }

		public ActionStartedChange(ILivingObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ActionStartedChangeData() { ObjectID = ObjectID.GetID(this.Object), ActionStartEvent = this.ActionStartEvent };
		}
	}

	public class ActionProgressChange : ObjectChange
	{
		public ActionProgressEvent ActionProgressEvent { get; set; }

		public ActionProgressChange(ILivingObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ActionProgressChangeData() { ObjectID = ObjectID.GetID(this.Object), ActionProgressEvent = this.ActionProgressEvent };
		}
	}

	public class ActionDoneChange : ObjectChange
	{
		public ActionDoneEvent ActionDoneEvent { get; set; }

		public ActionDoneChange(ILivingObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ActionDoneChangeData() { ObjectID = ObjectID.GetID(this.Object), ActionDoneEvent = this.ActionDoneEvent };
		}
	}

	public class WearChange : ObjectChange
	{
		public IItemObject Wearable { get; private set; }
		public ArmorSlot Slot { get; private set; }

		public WearChange(ILivingObject wearer, ArmorSlot slot, IItemObject wearable)
			: base(wearer)
		{
			this.Wearable = wearable;
			this.Slot = slot;
		}

		public override ChangeData ToChangeData()
		{
			return new WearChangeData() { ObjectID = ObjectID.GetID(this.Object), Slot = this.Slot, WearableID = ObjectID.GetID(this.Wearable) };
		}
	}

	public class WieldChange : ObjectChange
	{
		public IItemObject Weapon { get; private set; }

		public WieldChange(ILivingObject wearer, IItemObject weapon)
			: base(wearer)
		{
			this.Weapon = weapon;
		}

		public override ChangeData ToChangeData()
		{
			return new WieldChangeData() { ObjectID = ObjectID.GetID(this.Object), WeaponID = ObjectID.GetID(this.Weapon) };
		}
	}
}
