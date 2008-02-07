using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	interface IActor
	{
		void RemoveAction(GameAction action);
		GameAction GetCurrentAction();
		bool HasAction { get; }

		event Action ActionQueuedEvent;
	}
}
