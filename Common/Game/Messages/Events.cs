using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	[Serializable]
	public abstract class GameEvent
	{
	}

	[Serializable]
	public sealed class ActionStartEvent : GameEvent
	{
		public GameAction Action { get; set; }
		public ActionPriority Priority { get; set; }
	}

	[Serializable]
	public sealed class ActionProgressEvent : GameEvent
	{
		public int MagicNumber { get; set; }
		public int TotalTicks { get; set; }
		public int TicksUsed { get; set; }
	}

	[Serializable]
	public sealed class ActionDoneEvent : GameEvent
	{
		public int MagicNumber { get; set; }
		public ActionState State { get; set; }
		public GameAction Action { get; set; }
	}
}
