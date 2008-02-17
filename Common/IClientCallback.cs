using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public class ItemData
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int SymbolID { get; set; }
	}

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

		[OperationContract(IsOneWay = true)]
		void DeliverInventory(ItemData[] items);

	}
}
