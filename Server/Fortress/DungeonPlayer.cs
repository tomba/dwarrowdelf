using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server.Fortress
{
	[SaveGameObject]
	class DungeonPlayer : Player
	{
		public DungeonPlayer(int playerID, GameEngine engine)
			: base(playerID, engine)
		{
		}

		DungeonPlayer(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}
}
