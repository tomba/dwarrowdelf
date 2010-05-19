using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	public struct ShortPoint3D : IEquatable<ShortPoint3D>
	{
		readonly short m_x;
		readonly short m_y;
		readonly short m_z;

		public short X { get { return m_x; } }
		public short Y { get { return m_y; } }
		public short Z { get { return m_z; } }

		public ShortPoint3D(int x, int y, int z)
		{
			m_x = (short)x;
			m_y = (short)y;
			m_z = (short)z;
		}

		#region IEquatable<Location3D> Members

		public bool Equals(ShortPoint3D other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is ShortPoint3D))
				return false;

			ShortPoint3D l = (ShortPoint3D)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public static bool operator ==(ShortPoint3D left, ShortPoint3D right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(ShortPoint3D left, ShortPoint3D right)
		{
			return !(left == right);
		}

		public static ShortPoint3D operator +(ShortPoint3D left, IntVector3D right)
		{
			return new ShortPoint3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static ShortPoint3D operator +(ShortPoint3D left, IntVector right)
		{
			return new ShortPoint3D(left.X + right.X, left.Y + right.Y, left.Z);
		}

		public override int GetHashCode()
		{
			return (((ushort)m_x << 20)) | (((ushort)m_y << 10)) | ((ushort)m_z);
		}

		public override string ToString()
		{
			return String.Format(System.Globalization.CultureInfo.InvariantCulture,
				"ShortPoint3D({0}, {1}, {2})", X, Y, Z);
		}
	}
}
