using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	interface IActor
	{
		GameAction DequeueAction();
		GameAction PeekAction();
		bool HasAction { get; }

		event Action ActionQueuedEvent;
	}
}
