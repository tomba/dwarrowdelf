using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	public static class GameColors
	{
		public static readonly GameColor Red = new GameColor(255, 0, 0);
		public static readonly GameColor Blue = new GameColor(0, 255, 0);
		public static readonly GameColor Green = new GameColor(0, 0, 255);
	}

	[DataContract]
	public struct GameColor : IEquatable<GameColor>
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
			return String.Format(System.Globalization.CultureInfo.InvariantCulture,
				"Color({0:X},{1:X},{2:X})", m_r, m_g, m_b);
		}

		#region IEquatable<GameColor> Members

		public bool Equals(GameColor other)
		{
			return ((other.m_r == this.m_r) && (other.m_g == this.m_g) && (other.m_b == this.m_b));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is GameColor))
				return false;

			return Equals((GameColor)obj);
		}

		public override int GetHashCode()
		{
			return (m_r << 16) | (m_g << 8) | m_b;
		}

		public static bool operator== (GameColor left, GameColor right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GameColor left, GameColor right)
		{
			return !left.Equals(right);
		}
	}
}
