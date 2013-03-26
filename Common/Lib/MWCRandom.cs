using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	// basic mutiply with carry
	public struct MWCRandom
	{
		uint m_z;
		uint m_w;

		public MWCRandom(uint seed1, uint seed2)
		{
			m_z = seed1;
			m_w = seed2;
		}

		public MWCRandom(IntPoint2 p, int seed)
		{
			m_z = Hash.HashUInt32((uint)p.GetHashCode());
			m_w = (uint)seed;
			if (m_z == 0)
				m_z = 1;
			if (m_w == 0)
				m_w = 1;
		}

		public MWCRandom(IntPoint3 p, int seed)
		{
			m_z = Hash.HashUInt32((uint)p.GetHashCode());
			m_w = (uint)seed;
			if (m_z == 0)
				m_z = 1;
			if (m_w == 0)
				m_w = 1;
		}

		/// <summary>
		/// Get random Int32 [0, Int32.MaxValue)
		/// </summary>
		public int Next()
		{
			uint u = NextUint();
			return (int)(u * ((double)(Int32.MaxValue - 1) / UInt32.MaxValue));
		}

		/// <summary>
		/// Get random Int32 [0, exclusiveMax)
		/// </summary>
		public int Next(int exclusiveMax)
		{
			var d = NextDouble();
			return (int)(d * exclusiveMax);
		}

		/// <summary>
		/// Get random UInt32 [0, UInt32.MaxValue]
		/// </summary>
		public uint NextUint()
		{
			m_z = 36969 * (m_z & 65535) + (m_z >> 16);
			m_w = 18000 * (m_w & 65535) + (m_w >> 16);
			return (m_z << 16) + m_w;
		}

		/// <summary>
		/// Get random double [0.0, 1.0)
		/// </summary>
		public double NextDouble()
		{
			uint u = NextUint();
			return u * (1.0 / ((long)UInt32.MaxValue + 1));
		}
	}
}
