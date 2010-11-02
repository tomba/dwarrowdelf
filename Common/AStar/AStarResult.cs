using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.AStar
{
	public enum AStarStatus
	{
		Found,
		NotFound,
		LimitExceeded,
		Cancelled,
	}

	public class AStarResult
	{
		public IDictionary<IntPoint3D, AStarNode> Nodes { get; private set; }
		public AStarNode LastNode { get; private set; }
		public AStarStatus Status { get; private set; }

		internal AStarResult(IDictionary<IntPoint3D, AStarNode> nodes, AStarNode lastNode, AStarStatus status)
		{
			if (nodes == null)
				throw new ArgumentNullException();

			this.Nodes = nodes;
			this.LastNode = lastNode;
			this.Status = status;
		}

		public IEnumerable<Direction> GetPathReverse()
		{
			if (this.LastNode == null)
				yield break;

			AStarNode n = this.LastNode;
			while (n.Parent != null)
			{
				yield return (n.Parent.Loc - n.Loc).ToDirection();
				n = n.Parent;
			}
		}

		public IEnumerable<Direction> GetPath()
		{
			return GetPathReverse().Reverse().Select(d => d.Reverse());
		}
	}

}
