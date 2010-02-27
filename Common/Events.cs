using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	[Serializable]
	public abstract class Event
	{
	}

	[DataContract]
	[Serializable]
	public class TickChangeEvent : Event
	{
		[DataMember]
		public int TickNumber { get; set; }

		public TickChangeEvent(int tickNumber)
		{
			this.TickNumber = tickNumber;
		}

		public override string ToString()
		{
			return String.Format("TickChangeEvent({0})", this.TickNumber);
		}
	}

	[DataContract]
	[Serializable]
	public class ActionProgressEvent : Event
	{
		public int UserID { get; set; }
		[DataMember]
		public int TransactionID { get; set; }
		[DataMember]
		public int TicksLeft { get; set; }
		[DataMember]
		public bool Success { get; set; }

		public override string ToString()
		{
			return String.Format("ActionProgressEvent({0}, trid: {1}, left: {2}, ok: {3})",
				this.UserID, this.TransactionID, this.TicksLeft, this.Success);
		}
	}

	[DataContract]
	[Serializable]
	public class ActionRequiredEvent : Event
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }

		public override string ToString()
		{
			return String.Format("ActionRequiredEvent({0})", this.ObjectID);
		}
	}
}
