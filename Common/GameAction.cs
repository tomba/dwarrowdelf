using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
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
	public abstract class GameAction
	{
		// XXX. Server gen creates negative numbers, client positive
		public static Func<int> MagicNumberGenerator;

		public int MagicNumber { get; private set; }
		public ActionPriority Priority { get; private set; }

		protected GameAction(ActionPriority priority)
		{
			this.Priority = priority;
			this.MagicNumber = MagicNumberGenerator();
		}

		public sealed override string ToString()
		{
			return String.Format("{0} [{1}, {3}] ({2})", GetType().Name, this.Priority, this.GetParams(), this.MagicNumber);
		}

		protected abstract string GetParams();
	}

	[Serializable]
	public class MoveAction : GameAction
	{
		public Direction Direction { get; private set; }

		public MoveAction(Direction direction, ActionPriority priority)
			: base(priority)
		{
			this.Direction = direction;
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	public class WaitAction : GameAction
	{
		public int WaitTicks { get; private set; }

		public WaitAction(int ticks, ActionPriority priority)
			: base(priority)
		{
			this.WaitTicks = ticks;
		}

		protected override string GetParams()
		{
			return this.WaitTicks.ToString();
		}
	}

	[Serializable]
	public class DropAction : GameAction
	{
		public ObjectID[] ItemObjectIDs { get; private set; }

		public DropAction(IEnumerable<IGameObject> items, ActionPriority priority)
			: base(priority)
		{
			this.ItemObjectIDs = items.Select(i => i.ObjectID).ToArray();
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.ItemObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}

	[Serializable]
	public class GetAction : GameAction
	{
		public ObjectID[] ItemObjectIDs { get; private set; }

		public GetAction(IEnumerable<IGameObject> items, ActionPriority priority)
			: base(priority)
		{
			this.ItemObjectIDs = items.Select(i => i.ObjectID).ToArray();
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.ItemObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}

	[Serializable]
	public class MineAction : GameAction
	{
		public Direction Direction { get; private set; }

		public MineAction(Direction dir, ActionPriority priority)
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
	public class BuildItemAction : GameAction
	{
		// public object type etc

		public ObjectID[] SourceObjectIDs { get; private set; }

		public BuildItemAction(IEnumerable<IGameObject> sourceItems, ActionPriority priority)
			: base(priority)
		{
			this.SourceObjectIDs = sourceItems.Select(i => i.ObjectID).ToArray();
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.SourceObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}

}
