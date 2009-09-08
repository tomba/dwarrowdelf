using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using MyGame.ClientMsgs;

namespace MyGame
{
	public interface IClientCallback
	{
		[OperationContract(IsOneWay = true)]
		void LogOnReply();

		[OperationContract(IsOneWay = true)]
		void LogOnCharReply(ObjectID playerID);

		[OperationContract(IsOneWay = true)]
		void LogOffCharReply();

		[OperationContract(IsOneWay = true)]
		void DeliverMessage(ClientMsgs.Message message);

		[OperationContract(IsOneWay = true)]
		void DeliverMessages(IEnumerable<ClientMsgs.Message> messages);
/*
		[OperationContract(IsOneWay = true)]
		void DeliverMapTerrains(MapLocationTerrain[] locations);

		[OperationContract(IsOneWay = true)]
		void DeliverChanges(Change[] changes);


		[OperationContract(IsOneWay = true)]
		void DeliverInventory(ItemData[] items);

		[OperationContract(IsOneWay = true)]
		void TurnChange(int turnNumber);
*/
		[OperationContract(IsOneWay = true)]
		void TransactionDone(int transactionID);
	}
}
