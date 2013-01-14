using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	sealed class EnvTreeHandler
	{
		EnvironmentObject m_env;

		C5.IntervalHeap<SaplingNode> m_saplingPriorityQueue = new C5.IntervalHeap<SaplingNode>();
		int m_numTrees;

		public EnvTreeHandler(EnvironmentObject env)
		{
			m_env = env;
			ScanTreeTiles();
		}

		int GetTimeToGrowTree()
		{
			return m_env.World.Random.Next(100) + 10;
		}

		void ScanTreeTiles()
		{
			int now = m_env.World.TickNumber;

			m_numTrees = 0;

			foreach (var p in m_env.Size.Range())
			{
				var interior = m_env.GetInteriorID(p);

				switch (interior)
				{
					case InteriorID.Tree:
						m_numTrees++;
						break;

					case InteriorID.Sapling:
						m_numTrees++;

						int t = GetTimeToGrowTree();
						m_saplingPriorityQueue.Add(new SaplingNode(now + t, p));
						break;
				}
			}
		}

		public void AddTree()
		{
			m_numTrees++;
		}

		public void RemoveTree()
		{
			m_numTrees--;

			Debug.Assert(m_numTrees >= 0);
		}

		public void Tick()
		{
			int now = m_env.World.TickNumber;

			while (m_saplingPriorityQueue.Count > 0)
			{
				var node = m_saplingPriorityQueue.FindMin();

				if (node.Time > now)
					break;

				m_saplingPriorityQueue.DeleteMin();

				var td = m_env.GetTileData(node.Location);

				if (td.InteriorID == InteriorID.Sapling)
				{
					if (m_env.HasContents(node.Location))
					{
						int t = GetTimeToGrowTree();
						m_saplingPriorityQueue.Add(new SaplingNode(now + t, node.Location));
					}
					else
					{
						td.InteriorID = InteriorID.Tree;
						m_env.SetTileData(node.Location, td);
					}
				}
			}

			int normalNumTrees = m_env.Width * m_env.Height / 10;

			if (m_numTrees < normalNumTrees)
			{
				// XXX add saplings
			}
		}

		sealed class SaplingNode : IComparable<SaplingNode>
		{
			public int Time { get; private set; }
			public IntPoint3 Location { get; private set; }

			public SaplingNode(int time, IntPoint3 p)
			{
				this.Time = time;
				this.Location = p;
			}

			public int CompareTo(SaplingNode other)
			{
				return this.Time.CompareTo(other.Time);
			}
		}
	}
}
