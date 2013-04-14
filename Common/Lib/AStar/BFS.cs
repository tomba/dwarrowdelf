using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public enum BFSStatus
	{
		None = 0,
		NotFound,
		LimitExceeded,
		Cancelled,
	}

	public interface IBFSTarget
	{
		bool GetIsTarget(IntPoint3 p);
		IEnumerable<Direction> GetValidDirs(IntPoint3 p);
	}

	/// <summary>
	/// Breadth-first search
	/// </summary>
	public sealed class BFS
	{
		public CancellationToken CancellationToken { get; set; }
		public int MaxNodeCount { get; set; }
		public BFSStatus Status { get; private set; }

		IBFSTarget m_target;

		HashSet<IntPoint3> m_map;
		Queue<IntPoint3> m_queue;

		public BFS(IEnumerable<IntPoint3> initialLocations, IBFSTarget target)
		{
			this.MaxNodeCount = 200000;
			this.CancellationToken = CancellationToken.None;

			m_target = target;
			m_map = new HashSet<IntPoint3>();
			m_queue = new Queue<IntPoint3>();

			foreach (var p in initialLocations)
			{
				m_map.Add(p);
				m_queue.Enqueue(p);
			}
		}

		public IEnumerable<IntPoint3> Find()
		{
			while (m_queue.Count > 0)
			{
				if (this.CancellationToken.IsCancellationRequested)
				{
					this.Status = BFSStatus.Cancelled;
					yield break;
				}

				if (m_map.Count > this.MaxNodeCount)
				{
					this.Status = BFSStatus.LimitExceeded;
					yield break;
				}

				var p = m_queue.Dequeue();

				if (m_target.GetIsTarget(p))
					yield return p;

				CheckNeighbors(p);
			}

			this.Status = BFSStatus.NotFound;
		}

		void CheckNeighbors(IntPoint3 parent)
		{
			foreach (var dir in m_target.GetValidDirs(parent))
			{
				IntPoint3 child = parent + new IntVector3(dir);

				if (m_map.Contains(child) == false)
				{
					m_map.Add(child);
					m_queue.Enqueue(child);
				}
			}
		}
	}
}
