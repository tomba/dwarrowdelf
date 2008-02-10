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
		void LoginReply(ObjectID playerID, int visionRange);

		[OperationContract(IsOneWay = true)]
		void DeliverMapTerrains(MapLocationTerrain[] locations);

		[OperationContract(IsOneWay = true)]
		void DeliverChanges(Change[] changes);

		[OperationContract(IsOneWay = true)]
		void TransactionDone(int transactionID);
	}
}
