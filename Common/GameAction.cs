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
		Lowest,
		Idle,
		Normal,
		High,
	}

	[Serializable]
	[GameObject(UseRef = true)]
	public abstract class GameAction
	{
		// XXX. Server gen creates negative numbers, client positive
		public static Func<int> MagicNumberGenerator;

		[GameProperty]
		public int MagicNumber { get; private set; }
		[GameProperty]
		public ActionPriority Priority { get; private set; }

		protected GameAction(ActionPriority priority)
		{
			this.Priority = priority;
			this.MagicNumber = MagicNumberGenerator();
		}

		protected GameAction(GameSerializationContext ctx)
		{
		}

		public sealed override string ToString()
		{
			return String.Format("{0} [{1}, {3}] ({2})", GetType().Name, this.Priority, this.GetParams(), this.MagicNumber);
		}

		protected abstract string GetParams();
	}

	[Serializable]
	[GameObject(UseRef = true)]
	public class MoveAction : GameAction
	{
		[GameProperty]
		public Direction Direction { get; private set; }

		public MoveAction(Direction direction, ActionPriority priority)
			: base(priority)
		{
			this.Direction = direction;
		}

		protected MoveAction(GameSerializationContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[GameObject(UseRef = true)]
	public class WaitAction : GameAction
	{
		[GameProperty]
		public int WaitTicks { get; private set; }

		public WaitAction(int ticks, ActionPriority priority)
			: base(priority)
		{
			this.WaitTicks = ticks;
		}

		protected WaitAction(GameSerializationContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.WaitTicks.ToString();
		}
	}

	[Serializable]
	[GameObject(UseRef = true)]
	public class DropAction : GameAction
	{
		[GameProperty]
		public ObjectID[] ItemObjectIDs { get; private set; }

		public DropAction(IEnumerable<IGameObject> items, ActionPriority priority)
			: base(priority)
		{
			this.ItemObjectIDs = items.Select(i => i.ObjectID).ToArray();
		}

		protected DropAction(GameSerializationContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.ItemObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}

	[Serializable]
	[GameObject(UseRef = true)]
	public class GetAction : GameAction
	{
		[GameProperty]
		public ObjectID[] ItemObjectIDs { get; private set; }

		public GetAction(IEnumerable<IGameObject> items, ActionPriority priority)
			: base(priority)
		{
			this.ItemObjectIDs = items.Select(i => i.ObjectID).ToArray();
		}

		protected GetAction(GameSerializationContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.ItemObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}


	[Serializable]
	[GameObject(UseRef = true)]
	public class ConsumeAction : GameAction
	{
		[GameProperty]
		public ObjectID ItemObjectID { get; private set; }

		public ConsumeAction(IGameObject consumable, ActionPriority priority)
			: base(priority)
		{
			this.ItemObjectID = consumable.ObjectID;
		}

		protected ConsumeAction(GameSerializationContext ctx)
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
	[GameObject(UseRef = true)]
	public class MineAction : GameAction
	{
		[GameProperty]
		public Direction Direction { get; private set; }
		[GameProperty]
		public MineActionType MineActionType { get; private set; }

		public MineAction(Direction dir, MineActionType mineActionType, ActionPriority priority)
			: base(priority)
		{
			this.Direction = dir;
			this.MineActionType = mineActionType;
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[GameObject(UseRef = true)]
	public class FellTreeAction : GameAction
	{
		[GameProperty]
		public Direction Direction { get; private set; }

		public FellTreeAction(Direction dir, ActionPriority priority)
			: base(priority)
		{
			this.Direction = dir;
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[GameObject(UseRef = true)]
	public class BuildItemAction : GameAction
	{
		// public object type etc

		[GameProperty]
		public ObjectID[] SourceObjectIDs { get; private set; }
		[GameProperty]
		public ItemID DstItemID { get; private set; }

		public BuildItemAction(IEnumerable<IGameObject> sourceItems, ItemID dstItemID, ActionPriority priority)
			: base(priority)
		{
			this.SourceObjectIDs = sourceItems.Select(i => i.ObjectID).ToArray();
			this.DstItemID = dstItemID;
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.SourceObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}

	[Serializable]
	[GameObject(UseRef = true)]
	public class AttackAction : GameAction
	{
		[GameProperty]
		public ObjectID Target { get; private set; }

		public AttackAction(ILiving target, ActionPriority priority)
			: base(priority)
		{
			this.Target = target.ObjectID;
		}

		protected override string GetParams()
		{
			return this.Target.ToString();
		}
	}
}
