﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PerfTest
{
	abstract class IntPointIntTestBase
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected static IntPoint3D Add(IntPoint3D a, IntPoint3D b)
		{
			return new IntPoint3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		#region IntPoint
		public struct IntPoint3D : IEquatable<IntPoint3D>
		{
			readonly int m_x;
			readonly int m_y;
			readonly int m_z;

			public int X { get { return m_x; } }
			public int Y { get { return m_y; } }
			public int Z { get { return m_z; } }

			public IntPoint3D(int x, int y, int z)
			{
				m_x = x;
				m_y = y;
				m_z = z;
			}

			#region IEquatable<Location3D> Members

			public bool Equals(IntPoint3D other)
			{
				return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
			}

			#endregion

			public override bool Equals(object obj)
			{
				if (!(obj is IntPoint3D))
					return false;

				IntPoint3D l = (IntPoint3D)obj;
				return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
			}

			public static bool operator ==(IntPoint3D left, IntPoint3D right)
			{
				return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
			}

			public static bool operator !=(IntPoint3D left, IntPoint3D right)
			{
				return !(left == right);
			}


			public override int GetHashCode()
			{
				// 8 bits for Z, 12 bits for X/Y
				return (this.Z << 24) | (this.Y << 12) | (this.X << 0);
			}

			public static IEnumerable<IntPoint3D> Range(int x, int y, int z, int width, int height, int depth)
			{
				int max_x = x + width;
				int max_y = y + height;
				int max_z = z + depth;
				for (; z < max_z; ++z)
					for (; y < max_y; ++y)
						for (; x < max_x; ++x)
							yield return new IntPoint3D(x, y, z);
			}

			public static IEnumerable<IntPoint3D> Range(int width, int height, int depth)
			{
				for (int z = 0; z < depth; ++z)
					for (int y = 0; y < height; ++y)
						for (int x = 0; x < width; ++x)
							yield return new IntPoint3D(x, y, z);
			}

			public static IntPoint3D Center(IEnumerable<IntPoint3D> points)
			{
				int x, y, z;
				int count = 0;
				x = y = z = 0;

				foreach (var p in points)
				{
					x += p.X;
					y += p.Y;
					z += p.Z;
					count++;
				}

				return new IntPoint3D(x / count, y / count, z / count);
			}
		}
		#endregion
	}

	abstract class IntPointShortTestBase
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected static IntPoint3D Add(IntPoint3D a, IntPoint3D b)
		{
			return new IntPoint3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		#region IntPoint
		public struct IntPoint3D : IEquatable<IntPoint3D>
		{
			readonly short m_x;
			readonly short m_y;
			readonly short m_z;

			public int X { get { return m_x; } }
			public int Y { get { return m_y; } }
			public int Z { get { return m_z; } }

			public IntPoint3D(int x, int y, int z)
			{
				m_x = (short)x;
				m_y = (short)y;
				m_z = (short)z;
			}

			#region IEquatable<Location3D> Members

			public bool Equals(IntPoint3D other)
			{
				return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
			}

			#endregion

			public override bool Equals(object obj)
			{
				if (!(obj is IntPoint3D))
					return false;

				IntPoint3D l = (IntPoint3D)obj;
				return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
			}

			public static bool operator ==(IntPoint3D left, IntPoint3D right)
			{
				return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
			}

			public static bool operator !=(IntPoint3D left, IntPoint3D right)
			{
				return !(left == right);
			}


			public override int GetHashCode()
			{
				// 8 bits for Z, 12 bits for X/Y
				return (this.Z << 24) | (this.Y << 12) | (this.X << 0);
			}

			public static IEnumerable<IntPoint3D> Range(int x, int y, int z, int width, int height, int depth)
			{
				int max_x = x + width;
				int max_y = y + height;
				int max_z = z + depth;
				for (; z < max_z; ++z)
					for (; y < max_y; ++y)
						for (; x < max_x; ++x)
							yield return new IntPoint3D(x, y, z);
			}

			public static IEnumerable<IntPoint3D> Range(int width, int height, int depth)
			{
				for (int z = 0; z < depth; ++z)
					for (int y = 0; y < height; ++y)
						for (int x = 0; x < width; ++x)
							yield return new IntPoint3D(x, y, z);
			}

			public static IntPoint3D Center(IEnumerable<IntPoint3D> points)
			{
				int x, y, z;
				int count = 0;
				x = y = z = 0;

				foreach (var p in points)
				{
					x += p.X;
					y += p.Y;
					z += p.Z;
					count++;
				}

				return new IntPoint3D(x / count, y / count, z / count);
			}
		}
		#endregion
	}

	abstract class IntPointLongTestBase
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected static IntPoint3D Add(IntPoint3D a, IntPoint3D b)
		{
			return new IntPoint3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		#region IntPoint
		public struct IntPoint3D : IEquatable<IntPoint3D>
		{
			// X, Y, Z: 16 bits, from -32768 to 32767
			// X: bits 0-15, Y: bits 16-31, Z: bits 48-61
			readonly long m_value;

			public int X { get { return (((int)m_value) << 16) >> 16; } }
			public int Y { get { return ((int)m_value) >> 16; } }
			public int Z { get { return (int)(m_value >> 48); } }

			public IntPoint3D(int x, int y, int z)
			{
				m_value =
					((long)(x & 0xffff) << 0) |
					((long)(y & 0xffff) << 16) |
					((long)(z & 0xffff) << 48);
			}

			#region IEquatable<Location3D> Members

			public bool Equals(IntPoint3D other)
			{
				return m_value == other.m_value;
			}

			#endregion

			public override bool Equals(object obj)
			{
				if (!(obj is IntPoint3D))
					return false;

				IntPoint3D l = (IntPoint3D)obj;
				return m_value == l.m_value;
			}

			public static bool operator ==(IntPoint3D left, IntPoint3D right)
			{
				return left.m_value == right.m_value;
			}

			public static bool operator !=(IntPoint3D left, IntPoint3D right)
			{
				return !(left == right);
			}


			public override int GetHashCode()
			{
				// 8 bits for Z, 12 bits for X/Y
				return (this.Z << 24) | (this.Y << 12) | (this.X << 0);
			}

			public static IEnumerable<IntPoint3D> Range(int x, int y, int z, int width, int height, int depth)
			{
				int max_x = x + width;
				int max_y = y + height;
				int max_z = z + depth;
				for (; z < max_z; ++z)
					for (; y < max_y; ++y)
						for (; x < max_x; ++x)
							yield return new IntPoint3D(x, y, z);
			}

			public static IEnumerable<IntPoint3D> Range(int width, int height, int depth)
			{
				for (int z = 0; z < depth; ++z)
					for (int y = 0; y < height; ++y)
						for (int x = 0; x < width; ++x)
							yield return new IntPoint3D(x, y, z);
			}

			public static IntPoint3D Center(IEnumerable<IntPoint3D> points)
			{
				int x, y, z;
				int count = 0;
				x = y = z = 0;

				foreach (var p in points)
				{
					x += p.X;
					y += p.Y;
					z += p.Z;
					count++;
				}

				return new IntPoint3D(x / count, y / count, z / count);
			}
		}
		#endregion
	}

	abstract class IntPointInt2TestBase
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected static IntPoint3D Add(IntPoint3D a, IntPoint3D b)
		{
			return new IntPoint3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		#region IntPoint
		public struct IntPoint3D : IEquatable<IntPoint3D>
		{
			readonly int m_value1;
			readonly int m_value2;

			// X: 32 bits
			// Y: 24 bits
			// Z: 8 bits
			const int y_width = 24;
			const int z_width = 8;
			const int y_mask = (1 << y_width) - 1;
			const int z_mask = (1 << z_width) - 1;
			const int z_shift = 24;

			public int X { get { return m_value1; } }
			public int Y { get { return (m_value2 << 8) >> 8; } }
			public int Z { get { return m_value2 >> 24; } }

			public IntPoint3D(int x, int y, int z)
			{
				m_value1 = x;
				m_value2 = (y & y_mask) | ((z & z_mask) << z_shift);
			}

			#region IEquatable<Location3D> Members

			public bool Equals(IntPoint3D other)
			{
				return (m_value1 == other.m_value1) && (m_value2 == other.m_value2);
			}

			#endregion

			public override bool Equals(object obj)
			{
				if (!(obj is IntPoint3D))
					return false;

				IntPoint3D l = (IntPoint3D)obj;
				return (m_value1 == l.m_value1) && (m_value2 == l.m_value2);
			}

			public static bool operator ==(IntPoint3D left, IntPoint3D right)
			{
				return (left.m_value1 == right.m_value1) && (left.m_value2 == right.m_value2);
			}

			public static bool operator !=(IntPoint3D left, IntPoint3D right)
			{
				return !(left == right);
			}


			public override int GetHashCode()
			{
				// 8 bits for Z, 12 bits for X/Y
				return (this.Z << 24) | (this.Y << 12) | (this.X << 0);
			}

			public static IEnumerable<IntPoint3D> Range(int x, int y, int z, int width, int height, int depth)
			{
				int max_x = x + width;
				int max_y = y + height;
				int max_z = z + depth;
				for (; z < max_z; ++z)
					for (; y < max_y; ++y)
						for (; x < max_x; ++x)
							yield return new IntPoint3D(x, y, z);
			}

			public static IEnumerable<IntPoint3D> Range(int width, int height, int depth)
			{
				for (int z = 0; z < depth; ++z)
					for (int y = 0; y < height; ++y)
						for (int x = 0; x < width; ++x)
							yield return new IntPoint3D(x, y, z);
			}

			public static IntPoint3D Center(IEnumerable<IntPoint3D> points)
			{
				int x, y, z;
				int count = 0;
				x = y = z = 0;

				foreach (var p in points)
				{
					x += p.X;
					y += p.Y;
					z += p.Z;
					count++;
				}

				return new IntPoint3D(x / count, y / count, z / count);
			}
		}
		#endregion
	}
}
