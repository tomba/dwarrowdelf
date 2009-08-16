using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public interface IActor
	{
		void RemoveAction(GameAction action);
		GameAction GetCurrentAction();
		bool HasAction { get; }
		bool IsInteractive { get; }
		void ReportAction(bool done, bool success);

		event Action ActionQueuedEvent;
	}
}
