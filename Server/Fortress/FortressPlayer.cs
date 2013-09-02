using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server.Fortress
{
	[SaveGameObject]
	class FortressPlayer : Player
	{
		[SaveGameProperty]
		public EnvObserver EnvObserver { get; private set; }

		public FortressPlayer(int playerID, GameEngine engine, EnvironmentObject env)
			: base(playerID, engine)
		{
			this.EnvObserver = new EnvObserver(env);
		}
	}
}
