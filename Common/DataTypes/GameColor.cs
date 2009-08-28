using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public struct GameColor
	{
		[DataMember]
		byte m_r, m_g, m_b;

		public GameColor(byte r, byte g, byte b)
		{
			m_r = r;
			m_g = g;
			m_b = b;
		}

		public byte R { get { return m_r; } }
		public byte G { get { return m_g; } }
		public byte B { get { return m_b; } }

		public override string ToString()
		{
			return String.Format("Color({0:X},{1:X},{2:X})", m_r, m_g, m_b);
		}
	}
}
