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

		public GameAction(GameObject target)
		{
			m_target = target;
			this.ObjectID = m_target.ObjectID;
		}
	}

	[DataContract]
	public class MoveAction: GameAction
	{
		[DataMember]
		public Direction Direction { get; set; }

		public MoveAction(GameObject target, Direction direction)
			: base(target)
		{
			this.Direction = direction;
		}
	}
	[DataContract]
	public class WaitAction : GameAction
	{
		[DataMember]
		public int Turns { get; set; }

		public WaitAction(GameObject target, int turns)
			: base(target)
		{
			this.Turns = turns;
		}
	}


}
