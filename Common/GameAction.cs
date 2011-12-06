using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	public enum ActionState
	{
		/// <summary>
		/// None
		/// </summary>
		None,

		/// <summary>
		/// Action progressing ok
		/// </summary>
		Ok,

		/// <summary>
		/// Action failed
		/// </summary>
		Fail,

		/// <summary>
		/// Action aborted by somebody
		/// </summary>
		Abort,

		/// <summary>
		/// Action done successfully
		/// </summary>
		Done,
	}

	public enum ActionPriority
	{
		Undefined = 0,
		Idle,
		User,
		High,
	}

	[Serializable]
	[SaveGameObjectByRef]
	public abstract class GameAction
	{
		// XXX. Server gen creates negative numbers, client positive
		public static Func<int> MagicNumberGenerator;

		[SaveGameProperty]
		public int MagicNumber { get; private set; }

		protected GameAction()
		{
			this.MagicNumber = MagicNumberGenerator();
		}

		protected GameAction(SaveGameContext ctx)
		{
		}

		public sealed override string ToString()
		{
			return String.Format("{0} ({1})", GetType().Name, this.GetParams());
		}

		protected abstract string GetParams();
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class MoveAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }

		public MoveAction(Direction direction)
		{
			this.Direction = direction;
		}

		protected MoveAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class WaitAction : GameAction
	{
		[SaveGameProperty]
		public int WaitTicks { get; private set; }

		public WaitAction(int ticks)
		{
			this.WaitTicks = ticks;
		}

		protected WaitAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.WaitTicks.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class DropAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID[] ItemObjectIDs { get; private set; }

		public DropAction(IEnumerable<IMovableObject> items)
		{
			this.ItemObjectIDs = items.Select(i => i.ObjectID).ToArray();
		}

		protected DropAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.ItemObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class GetAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID[] ItemObjectIDs { get; private set; }

		public GetAction(IEnumerable<IMovableObject> items)
		{
			this.ItemObjectIDs = items.Select(i => i.ObjectID).ToArray();
		}

		protected GetAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.ItemObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}


	[Serializable]
	[SaveGameObjectByRef]
	public class ConsumeAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID ItemObjectID { get; private set; }

		public ConsumeAction(IMovableObject consumable)
		{
			this.ItemObjectID = consumable.ObjectID;
		}

		protected ConsumeAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.ItemObjectID.ToString();
		}
	}

	public enum MineActionType
	{
		/// <summary>
		/// Mine can be done for planar directions, and the roof of the mined tile stays,
		/// or up, if the current tile has stairs
		/// </summary>
		Mine,
		/// <summary>
		/// Stairs can be created for planar directions or down. To create stairs up, we need stairs already in the current tile.
		/// </summary>
		Stairs,
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class MineAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }
		[SaveGameProperty]
		public MineActionType MineActionType { get; private set; }

		public MineAction(Direction dir, MineActionType mineActionType)
		{
			this.Direction = dir;
			this.MineActionType = mineActionType;
		}

		protected MineAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class FellTreeAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }

		public FellTreeAction(Direction dir)
		{
			this.Direction = dir;
		}

		protected FellTreeAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class BuildItemAction : GameAction
	{
		// public object type etc

		[SaveGameProperty]
		public ObjectID[] SourceObjectIDs { get; private set; }
		[SaveGameProperty]
		public ItemID DstItemID { get; private set; }

		public BuildItemAction(IEnumerable<IMovableObject> sourceItems, ItemID dstItemID)
		{
			this.SourceObjectIDs = sourceItems.Select(i => i.ObjectID).ToArray();
			this.DstItemID = dstItemID;
		}

		protected BuildItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.SourceObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class AttackAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID Target { get; private set; }

		public AttackAction(ILivingObject target)
		{
			this.Target = target.ObjectID;
		}

		protected AttackAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Target.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class ConstructBuildingAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID EnvironmentID { get; private set; }
		[SaveGameProperty]
		public IntRectZ Area { get; private set; }
		[SaveGameProperty]
		public BuildingID BuildingID { get; private set; }

		public ConstructBuildingAction(IEnvironmentObject env, IntRectZ area, BuildingID buildingID)
		{
			this.EnvironmentID = env.ObjectID;
			this.Area = area;
			this.BuildingID = buildingID;
		}

		protected ConstructBuildingAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.EnvironmentID.ToString(), this.Area.ToString(), this.BuildingID.ToString());
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public class DestructBuildingAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID BuildingID { get; private set; }

		public DestructBuildingAction(ObjectID buildingID)
		{
			this.BuildingID = buildingID;
		}

		protected DestructBuildingAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.BuildingID.ToString();
		}
	}
}
