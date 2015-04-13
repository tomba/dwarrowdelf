using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public class VisionMap
	{
		BitArray m_bits;
		int m_visionRange;
		int m_side;

		public VisionMap(int visionRange)
		{
			m_visionRange = visionRange;
			m_side = visionRange * 2 + 1;
			m_bits = new BitArray(m_side * m_side * m_side);
		}

		public void Clear()
		{
			m_bits.SetAll(false);
		}

		public bool this[int x, int y, int z]
		{
			get
			{
				x += m_visionRange;
				y += m_visionRange;
				z += m_visionRange;

				int idx = z * m_side * m_side + y * m_side + x;

				return m_bits.Get(idx);
			}

			set
			{
				x += m_visionRange;
				y += m_visionRange;
				z += m_visionRange;

				int idx = z * m_side * m_side + y * m_side + x;

				m_bits.Set(idx, value);
			}
		}

		public bool this[IntVector3 p]
		{
			get { return this[p.X, p.Y, p.Z]; }
			set { this[p.X, p.Y, p.Z] = value; }
		}

		public IEnumerable<KeyValuePair<IntVector3, bool>> GetIndexValueEnumerable()
		{
			for (int z = -m_visionRange; z <= m_visionRange; z++)
			{
				for (int y = -m_visionRange; y <= m_visionRange; y++)
				{
					for (int x = -m_visionRange; x <= m_visionRange; x++)
					{
						yield return new KeyValuePair<IntVector3, bool>(new IntVector3(x, y, z), this[x, y, z]);
					}
				}
			}
		}
	}
}
