using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PerfTest
{
	class ArrayAccessTestSuite : TestSuite
	{
		const int c_widthExp = 11;
		const int c_width = 1 << c_widthExp;
		const int c_heightExp = 11;
		const int c_height = 1 << c_heightExp;
		const int c_depth = 32;

		public override void DoTests()
		{
			RunTest(new Safe1DimArrayAccessTest());
			RunTest(new Safe3DimArrayAccessTest());
			RunTest(new Unsafe1DimArrayAccessTest());
			RunTest(new Unsafe3DimArrayAccessTest());
		}

		class Safe1DimArrayAccessTest : ITest
		{
			int[] m_array = new int[c_width * c_height * c_depth];

			public void DoTest(int loops)
			{
				for (int test = 0; test < loops; test++)
					ArrayAccess(m_array);
			}

			static int ArrayAccess(int[] a)
			{
				int sum = 0;

				for (int z = 0; z < c_depth; z++)
				{
					int zBase = z << c_widthExp << c_heightExp;
					for (int y = 0; y < c_height; y++)
					{
						int yBase = y << c_widthExp;
						for (int x = 0; x < c_width; x++)
						{
							int idx = x + yBase + zBase;
							sum += a[idx];
						}
					}
				}

				return sum;
			}
		}

		class Safe3DimArrayAccessTest : ITest
		{
			int[, ,] m_array = new int[c_depth, c_height, c_width];

			public void DoTest(int loops)
			{
				for (int test = 0; test < loops; test++)
					ArrayAccess(m_array);
			}

			static int ArrayAccess(int[, ,] a)
			{
				int sum = 0;

				for (int z = 0; z < c_depth; z++)
					for (int y = 0; y < c_height; y++)
						for (int x = 0; x < c_width; x++)
							sum += a[z, y, x];

				return sum;
			}
		}

		class Unsafe1DimArrayAccessTest : ITest
		{
			int[] m_array = new int[c_width * c_height * c_depth];

			public void DoTest(int loops)
			{
				for (int test = 0; test < loops; test++)
					ArrayAccess(m_array);
			}

			static unsafe int ArrayAccess(int[] a)
			{
				int sum = 0;

				fixed (int* pi = a)
				{
					for (int z = 0; z < c_depth; z++)
					{
						int zBase = z << c_widthExp << c_heightExp;
						for (int y = 0; y < c_height; y++)
						{
							int yBase = y << c_widthExp;
							for (int x = 0; x < c_width; x++)
							{
								int idx = x + yBase + zBase;
								sum += pi[idx];
							}
						}
					}

					return sum;
				}
			}
		}

		class Unsafe3DimArrayAccessTest : ITest
		{
			int[, ,] m_array = new int[c_depth, c_height, c_width];

			public void DoTest(int loops)
			{
				for (int test = 0; test < loops; test++)
					ArrayAccess(m_array);
			}

			static unsafe int ArrayAccess(int[, ,] a)
			{
				int sum = 0;

				fixed (int* pi = a)
				{
					for (int z = 0; z < c_depth; z++)
					{
						int zBase = z << c_widthExp << c_heightExp;
						for (int y = 0; y < c_height; y++)
						{
							int yBase = y << c_widthExp;
							for (int x = 0; x < c_width; x++)
							{
								int idx = x + yBase + zBase;
								sum += pi[idx];
							}
						}
					}

					return sum;
				}
			}
		}

	}
}
