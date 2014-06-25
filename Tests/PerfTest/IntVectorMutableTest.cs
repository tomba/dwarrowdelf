using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Dwarrowdelf;

namespace PerfTest
{
	class IntVectorMutableTestSuite : TestSuite
	{
		public override void DoTests()
		{
			RunTest(new IntVectorImmutable());
			RunTest(new IntVectorMutable());
		}

		class IntVectorImmutable : ITest
		{
			IntVector3 m_v;

			public void DoTest(int loops)
			{
				IntVector3 v = new IntVector3();

				for (int i = 0; i < loops; ++i)
				{
					v += new IntVector3(1, 2, 3);
					v += new IntVector3(2, 3, 4);
					v += new IntVector3(3, 4, 5);
					v += new IntVector3(4, 5, 6);
				}

				m_v = v;
			}
		}

		class IntVectorMutable : ITest
		{
			IntVector3Mutable m_v;

			public void DoTest(int loops)
			{
				IntVector3Mutable v = new IntVector3Mutable();

				for (int i = 0; i < loops; ++i)
				{
					v.Add(1, 2, 3);
					v.Add(2, 3, 4);
					v.Add(3, 4, 5);
					v.Add(4, 5, 6);
				}

				m_v = v;
			}
		}
	}
}

