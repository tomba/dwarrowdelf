using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	[Serializable]
	public class Region
	{
		List<IntBox> m_boxs = new List<IntBox>();

		public void Add(IntGrid2Z rect)
		{
			Add(new IntBox(rect));
		}

		public void Add(IntBox box)
		{
			m_boxs.Add(box);

		}

		public void Remove(IntBox box)
		{
			m_boxs.Remove(box);
		}

		public IntPoint3? Center
		{
			get
			{
				if (m_boxs.Count == 0)
					return null;

				var v = new IntVector3();
				int i = 0;

				foreach (var c in m_boxs)
				{
					v += new IntVector3(c.Center);
					i++;
				}

				v /= i;

				return new IntPoint3(v.X, v.Y, v.Z);
			}
		}

		public bool Contains(IntPoint3 p)
		{
			return m_boxs.Any(c => c.Contains(p));
		}
	}
}
