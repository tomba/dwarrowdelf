using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract, KnownType(typeof(MoveAction)), KnownType(typeof(WaitAction))]
	public abstract class GameAction
	{
		GameObject m_target;
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public int TransactionID { get; set; }

		public GameAction(int transID, GameObject target)
		{
			this.TransactionID = transID;
			m_target = target;
			this.ObjectID = m_target.ObjectID;
		}
	}

	[DataContract]
	public class MoveAction: GameAction
	{
		[DataMember]
		public Direction Direction { get; set; }

		public MoveAction(int transID, GameObject target, Direction direction)
			: base(transID, target)
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

		public WaitAction(int transID, GameObject target, int turns)
			: base(transID, target)
		{
			this.Turns = turns;
		}

		public override string ToString()
		{
			return String.Format("WaitAction({0})", this.Turns);
		}
	}


}
