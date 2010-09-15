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
		User,
		High,
	}

	[Serializable]
	public abstract class GameAction
	{
		public ActionPriority Priority { get; private set; }

		protected GameAction(ActionPriority priority)
		{
			this.Priority = priority;
		}
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

		public override string ToString()
		{
			return String.Format("MoveAction({0})", this.Direction);
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

		public override string ToString()
		{
			return String.Format("WaitAction({0})", this.WaitTicks);
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

		public override string ToString()
		{
			return String.Format("DropAction({0})",
				String.Join(", ", this.ItemObjectIDs.Select(i => i.ToString()).ToArray()));
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

		public override string ToString()
		{
			return String.Format("GetAction({0})",
				String.Join(", ", this.ItemObjectIDs.Select(i => i.ToString()).ToArray()));
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

		public override string ToString()
		{
			return String.Format("MineAction({0})", this.Direction);
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

		public override string ToString()
		{
			return String.Format("BuildItemAction()");
		}
	}

}
