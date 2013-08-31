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
		public GameAction Action;
		public ActionPriority Priority;
	}

	[Serializable]
	public sealed class ActionProgressEvent : GameEvent
	{
		public ActionGUID GUID;
		public int TotalTicks;
		public int TicksUsed;
	}

	[Serializable]
	public sealed class ActionDoneEvent : GameEvent
	{
		public ActionGUID GUID;
		public ActionState State;
	}
}
