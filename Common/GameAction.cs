using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	[Serializable]
	public abstract class GameAction
	{
		[DataMember]
		public ObjectID ActorObjectID { get; set; }
		[DataMember]
		public int TransactionID { get; set; }

		public int UserID { get; set; }
		public int TicksLeft { get; set; }

		public GameAction()
		{
		}
	}

	[DataContract]
	[Serializable]
	public class NopAction : GameAction
	{
		public NopAction()
		{
		}

		public override string ToString()
		{
			return String.Format("NopAction()");
		}
	}

	[DataContract]
	[Serializable]
	public class MoveAction : GameAction
	{
		[DataMember]
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

	[DataContract]
	[Serializable]
	public class WaitAction : GameAction
	{
		[DataMember]
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

	[DataContract]
	[Serializable]
	public class DropAction : GameAction
	{
		[DataMember]
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

	[DataContract]
	[Serializable]
	public class GetAction : GameAction
	{
		[DataMember]
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

	[DataContract]
	[Serializable]
	public class MineAction : GameAction
	{
		[DataMember]
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

	[DataContract]
	[Serializable]
	public class BuildItemAction : GameAction
	{
		// [DataMember]
		// public object type etc

		[DataMember]
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
