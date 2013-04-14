using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public enum AStarStatus
	{
		Found,
		NotFound,
		LimitExceeded,
		Cancelled,
	}

	public sealed class AStarResult
	{
		public AStarNode LastNode { get; private set; }
		public AStarStatus Status { get; private set; }

		internal AStarResult(AStarStatus status, AStarNode lastNode)
		{
			this.LastNode = lastNode;
			this.Status = status;
		}

		public IEnumerable<IntPoint3> GetPathLocationsReverse()
		{
			for (AStarNode n = this.LastNode; n != null; n = n.Parent)
				yield return n.Loc;
		}

		public IEnumerable<Direction> GetPathReverse()
		{
			if (this.LastNode == null)
				yield break;

			for (AStarNode n = this.LastNode; n.Parent != null; n = n.Parent)
				yield return (n.Parent.Loc - n.Loc).ToDirection();
		}

		public IEnumerable<Direction> GetPath()
		{
			return GetPathReverse().Reverse().Select(d => d.Reverse());
		}
	}
}
