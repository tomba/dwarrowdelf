using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public class Region
	{
		List<IntCuboid> m_cuboids = new List<IntCuboid>();

		public void Add(IntRectZ rect)
		{
			Add(new IntCuboid(rect));
		}

		public void Add(IntCuboid cuboid)
		{
			m_cuboids.Add(cuboid);

		}

		public void Remove(IntCuboid cuboid)
		{
			m_cuboids.Remove(cuboid);
		}

		public IntPoint3? Center
		{
			get
			{
				if (m_cuboids.Count == 0)
					return null;

				var v = new IntVector3();
				int i = 0;

				foreach (var c in m_cuboids)
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
			return m_cuboids.Any(c => c.Contains(p));
		}
	}
}
