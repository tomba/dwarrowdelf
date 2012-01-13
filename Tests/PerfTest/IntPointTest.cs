using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PerfTest
{
	class IntPointTestSuite : TestSuite
	{

		public override void DoTests()
		{
			var tests = new ITest[] {
				new IntPointIntTest(),
				new IntPointShortTest(),
				new IntPointBitTest(),
			};

			foreach (var test in tests)
				RunTest(test);
		}

		class IntPointIntTest : ITest
		{
			public IntPoint3D m_result;

			public void DoTest(int loops)
			{
				while (loops-- > 0)
				{
					IntPoint3D r = new IntPoint3D();

					foreach (var p in IntPoint3D.Range(256, 256, 16))
					{
						r = new IntPoint3D(r.X + p.X, r.Y + p.Y, r.Z + p.Z);
					}

					m_result = r;
				}
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


		class IntPointShortTest : ITest
		{
			public IntPoint3D m_result;

			public void DoTest(int loops)
			{
				while (loops-- > 0)
				{
					IntPoint3D r = new IntPoint3D();

					foreach (var p in IntPoint3D.Range(256, 256, 16))
					{
						r = new IntPoint3D(r.X + p.X, r.Y + p.Y, r.Z + p.Z);
					}

					m_result = r;
				}
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

		class IntPointBitTest : ITest
		{
			public IntPoint3D m_result;

			public void DoTest(int loops)
			{
				while (loops-- > 0)
				{
					IntPoint3D r = new IntPoint3D();

					foreach (var p in IntPoint3D.Range(256, 256, 16))
					{
						r = new IntPoint3D(r.X + p.X, r.Y + p.Y, r.Z + p.Z);
					}

					m_result = r;
				}
			}

			#region IntPoint
			public struct IntPoint3D : IEquatable<IntPoint3D>
			{
				readonly int m_value;

				const int x_mask = (1 << 12) - 1;
				const int y_mask = (1 << 12) - 1;
				const int z_mask = (1 << 8) - 1;
				const int x_shift = 0;
				const int y_shift = 12;
				const int z_shift = 12 + 12;

				public int X { get { return (m_value >> x_shift) & x_mask; } }
				public int Y { get { return (m_value >> y_shift) & y_mask; } }
				public int Z { get { return (m_value >> z_shift) & z_mask; } }

				public IntPoint3D(int x, int y, int z)
				{
					m_value =
						((x & x_mask) << x_shift) |
						((y & y_mask) << y_shift) |
						((z & z_mask) << z_shift);
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

	}
}
