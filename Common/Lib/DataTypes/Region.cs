using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	[Serializable]
	public class Region
	{
		List<IntGrid3> m_boxs = new List<IntGrid3>();

		public void Add(IntGrid2Z rect)
		{
			Add(new IntGrid3(rect));
		}

		public void Add(IntGrid3 box)
		{
			m_boxs.Add(box);

		}

		public void Remove(IntGrid3 box)
		{
			m_boxs.Remove(box);
		}

		public IntVector3? Center
		{
			get
			{
				if (m_boxs.Count == 0)
					return null;

				var v = new IntVector3();
				int i = 0;

				foreach (var c in m_boxs)
				{
					v += c.Center;
					i++;
				}

				v /= i;

				return new IntVector3(v.X, v.Y, v.Z);
			}
		}

		public bool Contains(IntVector3 p)
		{
			return m_boxs.Any(c => c.Contains(p));
		}
	}
}
