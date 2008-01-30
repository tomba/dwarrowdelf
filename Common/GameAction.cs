using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract, KnownType(typeof(MoveAction))]
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
}
