using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
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
		public ObjectID ActorObjectID { get; set; }
		public int TransactionID { get; set; }
		public ActionPriority Priority { get; set; }

		[NonSerialized]
		int m_userId;

		[NonSerialized]
		int m_ticksLeft;

		public int UserID { get { return m_userId; } set { m_userId = value; } }
		public int TicksLeft { get { return m_ticksLeft; } set { m_ticksLeft = value; } }

		public GameAction()
		{
		}
	}

	[Serializable]
	public class MoveAction : GameAction
	{
		public Direction Direction { get; set; }

		public MoveAction(Direction direction)
		{
			this.Direction = direction;
		}

		public override string ToString()
		{
			return String.Format("MoveAction({0}, left {1})", this.Direction, this.TicksLeft);
		}
	}

	[Serializable]
	public class WaitAction : GameAction
	{
		public int WaitTicks { get; set; }

		public WaitAction(int ticks)
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
		public ObjectID[] ItemObjectIDs { get; set; }

		public DropAction(IEnumerable<IIdentifiable> items)
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
		public ObjectID[] ItemObjectIDs { get; set; }

		public GetAction(IEnumerable<IIdentifiable> items)
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
		public Direction Direction { get; set; }

		public MineAction(Direction dir)
		{
			this.Direction = dir;
		}

		public override string ToString()
		{
			return String.Format("MineAction({0}, turns: {1})", this.Direction, this.TicksLeft);
		}
	}

	[Serializable]
	public class BuildItemAction : GameAction
	{
		// public object type etc

		public ObjectID[] SourceObjectIDs { get; set; }

		public BuildItemAction(IEnumerable<IIdentifiable> sourceItems)
		{
			this.SourceObjectIDs = sourceItems.Select(i => i.ObjectID).ToArray();
		}

		public override string ToString()
		{
			return String.Format("BuildItemAction(turns: {0})", this.TicksLeft);
		}
	}

}
