using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract,
	KnownType(typeof(MoveAction)),
	KnownType(typeof(WaitAction)),
	KnownType(typeof(GetAction)),
	KnownType(typeof(DropAction)),
	]
	public abstract class GameAction
	{
		GameObject m_target;
		[DataMember]
		public ObjectID ActorObjectID { get; set; }
		[DataMember]
		public int TransactionID { get; set; }

		public GameAction(int transID, GameObject actor)
		{
			this.TransactionID = transID;
			m_target = actor;
			this.ActorObjectID = m_target.ObjectID;
		}
	}

	[DataContract]
	public class MoveAction: GameAction
	{
		[DataMember]
		public Direction Direction { get; set; }

		public MoveAction(int transID, GameObject actor, Direction direction)
			: base(transID, actor)
		{
			this.Direction = direction;
		}

		public override string ToString()
		{
			return String.Format("MoveAction({0})", this.Direction);
		}
	}

	[DataContract]
	public class WaitAction : GameAction
	{
		[DataMember]
		public int Turns { get; set; }

		public WaitAction(int transID, GameObject actor, int turns)
			: base(transID, actor)
		{
			this.Turns = turns;
		}

		public override string ToString()
		{
			return String.Format("WaitAction({0})", this.Turns);
		}
	}

	[DataContract]
	public class DropAction : GameAction
	{
		[DataMember]
		public ObjectID ItemObjectID { get; set; }

		public DropAction(int transID, GameObject actor, GameObject item)
			: base(transID, actor)
		{
			this.ItemObjectID = item.ObjectID;
		}

		public override string ToString()
		{
			return String.Format("DropAction({0})", this.ItemObjectID);
		}
	}

	[DataContract]
	public class GetAction : GameAction
	{
		[DataMember]
		public ObjectID ItemObjectID { get; set; }

		public GetAction(int transID, GameObject actor, GameObject item)
			: base(transID, actor)
		{
			this.ItemObjectID = item.ObjectID;
		}

		public override string ToString()
		{
			return String.Format("GetAction({0})", this.ItemObjectID);
		}
	}
}
