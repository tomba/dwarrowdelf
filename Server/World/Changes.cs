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

	public sealed class TickStartChange : Change
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

	public sealed class TurnStartChange : Change
	{
		// Sequential : Living who's turn it is
		// Simultaneous: null
		public LivingObject Living { get; private set; }

		public TurnStartChange(LivingObject living)
		{
			this.Living = living;
		}

		public override ChangeData ToChangeData()
		{
			var oid = this.Living != null ? this.Living.ObjectID : ObjectID.AnyObjectID;
			return new TurnStartChangeData() { LivingID = oid };
		}
	}

	public sealed class TurnEndChange : Change
	{
		// Sequential : Living who's turn it is
		// Simultaneous: null
		public LivingObject Living { get; private set; }

		public TurnEndChange(LivingObject living)
		{
			this.Living = living;
		}

		public override ChangeData ToChangeData()
		{
			var oid = this.Living != null ? this.Living.ObjectID : ObjectID.AnyObjectID;
			return new TurnEndChangeData() { LivingID = oid };
		}
	}

	public sealed class GameDateChange : Change
	{
		public int Year { get; private set; }
		public GameSeason Season { get; private set; }

		public GameDateChange(int Year, GameSeason season)
		{
			this.Year = Year;
			this.Season = season;
		}

		public override ChangeData ToChangeData()
		{
			return new GameDateChangeData() { Year = this.Year, Season = this.Season };
		}
	}

	public abstract class EnvironmentChange : Change
	{
		public EnvironmentObject Environment { get; private set; }

		protected EnvironmentChange(EnvironmentObject env)
		{
			this.Environment = env;
		}
	}

	public sealed class MapChange : EnvironmentChange
	{
		public IntVector3 Location { get; private set; }
		public TileData TileData { get; private set; }

		public MapChange(EnvironmentObject map, IntVector3 l, TileData tileData)
			: base(map)
		{
			this.Location = l;
			this.TileData = tileData;
		}

		public override ChangeData ToChangeData()
		{
			return new MapChangeData() { EnvironmentID = this.Environment.ObjectID, Location = this.Location, TileData = this.TileData };
		}
	}

	public abstract class ObjectChange : Change
	{
		public BaseObject Object { get; private set; }

		protected ObjectChange(BaseObject ob)
		{
			this.Object = ob;
		}
	}

	public sealed class ObjectCreatedChange : ObjectChange
	{
		public ObjectCreatedChange(BaseObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ObjectCreatedChangeData() { ObjectID = this.Object.ObjectID };
		}
	}

	public sealed class ObjectDestructedChange : ObjectChange
	{
		public ObjectDestructedChange(BaseObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ObjectDestructedChangeData() { ObjectID = this.Object.ObjectID };
		}
	}

	public abstract class PropertyChange : ObjectChange
	{
		public PropertyID PropertyID { get; private set; }

		protected PropertyChange(BaseObject ob, PropertyID propertyID)
			: base(ob)
		{
			this.PropertyID = propertyID;
		}
	}

	public sealed class PropertyValueChange : PropertyChange
	{
		public ValueType Value { get; private set; }

		public PropertyValueChange(BaseObject ob, PropertyID propertyID, ValueType value)
			: base(ob, propertyID)
		{
			this.Value = value;
		}

		public override ChangeData ToChangeData()
		{
			return new PropertyValueChangeData() { ObjectID = this.Object.ObjectID, PropertyID = this.PropertyID, Value = this.Value };
		}
	}

	public sealed class PropertyIntChange : PropertyChange
	{
		public int Value { get; private set; }

		public PropertyIntChange(BaseObject ob, PropertyID propertyID, int value)
			: base(ob, propertyID)
		{
			this.Value = value;
		}

		public override ChangeData ToChangeData()
		{
			return new PropertyIntChangeData() { ObjectID = this.Object.ObjectID, PropertyID = this.PropertyID, Value = this.Value };
		}
	}

	public sealed class PropertyStringChange : PropertyChange
	{
		public string Value { get; private set; }

		public PropertyStringChange(BaseObject ob, PropertyID propertyID, string value)
			: base(ob, propertyID)
		{
			this.Value = value;
		}

		public override ChangeData ToChangeData()
		{
			return new PropertyStringChangeData() { ObjectID = this.Object.ObjectID, PropertyID = this.PropertyID, Value = this.Value };
		}
	}

	public sealed class ObjectMoveChange : ObjectChange
	{
		public ContainerObject Source { get; private set; }
		public IntVector3 SourceLocation { get; private set; }

		public ContainerObject Destination { get; private set; }
		public IntVector3 DestinationLocation { get; private set; }

		public ObjectMoveChange(MovableObject mover, ContainerObject sourceEnv, IntVector3 sourceLocation,
			ContainerObject destinationEnv, IntVector3 destinationLocation)
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
				ObjectID = this.Object.ObjectID,
				SourceID = ObjectID.GetID(this.Source),
				SourceLocation = this.SourceLocation,
				DestinationID = ObjectID.GetID(this.Destination),
				DestinationLocation = this.DestinationLocation
			};
		}
	}

	public sealed class ObjectMoveLocationChange : ObjectChange
	{
		public IntVector3 SourceLocation { get; private set; }
		public IntVector3 DestinationLocation { get; private set; }

		public ObjectMoveLocationChange(MovableObject mover, IntVector3 sourceLocation, IntVector3 destinationLocation)
			: base(mover)
		{
			this.SourceLocation = sourceLocation;
			this.DestinationLocation = destinationLocation;
		}
		public override ChangeData ToChangeData()
		{
			return new ObjectMoveLocationChangeData()
			{
				ObjectID = this.Object.ObjectID,
				SourceLocation = this.SourceLocation,
				DestinationLocation = this.DestinationLocation
			};
		}
	}



	public sealed class SkillChange : ObjectChange
	{
		public SkillID SkillID { get; private set; }
		public byte Level { get; private set; }

		public SkillChange(LivingObject ob, SkillID skillID, byte level)
			: base(ob)
		{
			this.SkillID = skillID;
			this.Level = level;
		}

		public override ChangeData ToChangeData()
		{
			return new SkillChangeData() { ObjectID = this.Object.ObjectID, SkillID = this.SkillID, Level = this.Level };
		}
	}

	public sealed class ActionStartedChange : ObjectChange
	{
		public ActionStartEvent ActionStartEvent { get; set; }

		public ActionStartedChange(LivingObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ActionStartedChangeData() { ObjectID = this.Object.ObjectID, ActionStartEvent = this.ActionStartEvent };
		}
	}

	public sealed class ActionProgressChange : ObjectChange
	{
		public ActionProgressEvent ActionProgressEvent { get; set; }

		public ActionProgressChange(LivingObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ActionProgressChangeData() { ObjectID = this.Object.ObjectID, ActionProgressEvent = this.ActionProgressEvent };
		}
	}

	public sealed class ActionDoneChange : ObjectChange
	{
		public ActionDoneEvent ActionDoneEvent { get; set; }

		public ActionDoneChange(LivingObject ob)
			: base(ob)
		{
		}

		public override ChangeData ToChangeData()
		{
			return new ActionDoneChangeData() { ObjectID = this.Object.ObjectID, ActionDoneEvent = this.ActionDoneEvent };
		}
	}
}
