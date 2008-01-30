using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace MyGame
{
	public interface IClientCallback
	{
		[OperationContract(IsOneWay = true)]
		void LoginReply(ObjectID playerID);

		[OperationContract(IsOneWay = true)]
		void DeliverMapTerrains(MapLocation[] locations);

		[OperationContract(IsOneWay = true)]
		void MapChanged(Location l, int[] map);

		[OperationContract(IsOneWay = true)]
		void PlayerMoved(Location l);

		[OperationContract(IsOneWay = true)]
		void DeliverChanges(Change[] changes);
	}
}
