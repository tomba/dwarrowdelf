using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public sealed class GameFactory : MarshalByRefObject, IGameFactory
	{
		public IGame CreateGame(GameMode mode, string gameDir)
		{
			switch (mode)
			{
				case GameMode.Fortress:
					return new Game(new Fortress.Area(), gameDir);
			}

			throw new Exception();
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
