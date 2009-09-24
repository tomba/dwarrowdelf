using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace MyGame
{
	[ServiceContract(CallbackContract = typeof(IClientCallback), SessionMode = SessionMode.Required)]
	public interface IServerService
	{
		[OperationContract(IsOneWay=true)]
		void LogOn(string name);

		[OperationContract(IsOneWay = true)]
		void LogOff();

		[OperationContract(IsOneWay = true)]
		void SetTiles(ObjectID mapID, IntCube cube, int type);

		[OperationContract(IsOneWay = true)]
		void ProceedTurn();

		/* actions for livings */
		[OperationContract(IsOneWay = true)]
		void LogOnChar(string name);

		[OperationContract(IsOneWay = true)]
		void LogOffChar();

		[OperationContract(IsOneWay = true)]
		void EnqueueAction(GameAction action);
	}
}
