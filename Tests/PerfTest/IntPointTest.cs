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
				new IntPointIntForTest(),
				new IntPointShortForTest(),
				new IntPointLongForTest(),
				new IntPoint2IntForTest(),
				new IntPointIntForeachTest(),
				new IntPointShortForeachTest(),
			};

			foreach (var test in tests)
				RunTest(test);
		}

		class IntPointIntForTest : IntPointIntTestBase, ITest
		{
			public IntPoint3D m_p;

			public void DoTest(int loops)
			{
				while (loops-- > 0)
				{
					IntPoint3D r = new IntPoint3D();

					for (int z = 0; z < ZLOOPS; ++z)
						for (int y = 0; y < YLOOPS; ++y)
							for (int x = 0; x < XLOOPS; ++x)
							{
								var p = new IntPoint3D(x, y, z);
								var q = new IntPoint3D(p.X, -1, -1);
								if (r != q)
									r = new IntPoint3D(r.X + p.X, r.Y + p.Y, r.Z + p.Z);
							}

					m_p = r;
				}
			}
		}

		class IntPointShortForTest : IntPointShortTestBase, ITest
		{
			public IntPoint3D m_p;

			public void DoTest(int loops)
			{
				while (loops-- > 0)
				{
					IntPoint3D r = new IntPoint3D();

					for (int z = 0; z < ZLOOPS; ++z)
						for (int y = 0; y < YLOOPS; ++y)
							for (int x = 0; x < XLOOPS; ++x)
							{
								var p = new IntPoint3D(x, y, z);
								var q = new IntPoint3D(p.X, -1, -1);
								if (r != q)
									r = new IntPoint3D(r.X + p.X, r.Y + p.Y, r.Z + p.Z);
							}

					m_p = r;
				}
			}
		}

		class IntPointLongForTest : IntPointLongBase, ITest
		{
			public IntPoint3D m_p;

			public void DoTest(int loops)
			{
				while (loops-- > 0)
				{
					IntPoint3D r = new IntPoint3D();

					for (int z = 0; z < ZLOOPS; ++z)
						for (int y = 0; y < YLOOPS; ++y)
							for (int x = 0; x < XLOOPS; ++x)
							{
								var p = new IntPoint3D(x, y, z);
								var q = new IntPoint3D(p.X, -1, -1);
								if (r != q)
									r = new IntPoint3D(r.X + p.X, r.Y + p.Y, r.Z + p.Z);
							}

					m_p = r;
				}
			}
		}

		class IntPoint2IntForTest : IntPointInt2TestBase, ITest
		{
			public IntPoint3D m_p;

			public void DoTest(int loops)
			{
				while (loops-- > 0)
				{
					IntPoint3D r = new IntPoint3D();

					for (int z = 0; z < ZLOOPS; ++z)
						for (int y = 0; y < YLOOPS; ++y)
							for (int x = 0; x < XLOOPS; ++x)
							{
								var p = new IntPoint3D(x, y, z);
								var q = new IntPoint3D(p.X, -1, -1);
								if (r != q)
									r = new IntPoint3D(r.X + p.X, r.Y + p.Y, r.Z + p.Z);
							}

					m_p = r;
				}
			}
		}

		class IntPointIntForeachTest : IntPointIntTestBase, ITest
		{
			public IntPoint3D m_p;

			public void DoTest(int loops)
			{
				while (loops-- > 0)
				{
					IntPoint3D r = new IntPoint3D();

					foreach (var p in IntPoint3D.Range(XLOOPS, YLOOPS, ZLOOPS))
					{
						var q = new IntPoint3D(p.X, -1, -1);
						if (r != q)
							r = new IntPoint3D(r.X + p.X, r.Y + p.Y, r.Z + p.Z);
					}

					m_p = r;
				}
			}
		}

		class IntPointShortForeachTest : IntPointShortTestBase, ITest
		{
			public IntPoint3D m_p;

			public void DoTest(int loops)
			{
				while (loops-- > 0)
				{
					IntPoint3D r = new IntPoint3D();

					foreach (var p in IntPoint3D.Range(XLOOPS, YLOOPS, ZLOOPS))
					{
						var q = new IntPoint3D(p.X, -1, -1);
						if (r != q)
							r = new IntPoint3D(r.X + p.X, r.Y + p.Y, r.Z + p.Z);
					}

					m_p = r;
				}
			}
		}


	}
}
