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
		void Login(string name);

		[OperationContract(IsOneWay = true)]
		void Logout();

		[OperationContract(IsOneWay = true)]
		void DoAction(GameAction action);

		[OperationContract(IsOneWay = true)]
		void ToggleTile(IntPoint l);

		[OperationContract(IsOneWay = true)]
		void SetTiles(IntRect r, int type);
	}
}
