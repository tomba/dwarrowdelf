using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public interface IGameManager
	{
		void SetupLivingAsControllable(LivingObject living);
		LivingObject[] SetupWorldForNewPlayer(Player player);
	}
}
