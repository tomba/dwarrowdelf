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
	public class ActionStartEvent : GameEvent
	{
		public GameAction Action { get; set; }
		public ActionPriority Priority { get; set; }
		public int UserID { get; set; }
	}

	[Serializable]
	public class ActionProgressEvent : GameEvent
	{
		public int MagicNumber { get; set; }
		public int UserID { get; set; }
		public int TotalTicks { get; set; }
		public int TicksUsed { get; set; }
	}

	[Serializable]
	public class ActionDoneEvent : GameEvent
	{
		public int MagicNumber { get; set; }
		public int UserID { get; set; }
		public ActionState State { get; set; }
	}
}
