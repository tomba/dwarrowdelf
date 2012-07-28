using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PerfTest
{
	class GenericIFaceAccessTestSuite : TestSuite
	{
		public override void DoTests()
		{
			RunTest(new Test1Test());
			RunTest(new Test2Test());
			RunTest(new Test3Test());
		}

		interface IMyNode
		{
			void Test();
		}

		class MyNode : IMyNode
		{
			public int counter;

			[MethodImpl(MethodImplOptions.NoInlining)]
			public void Test()
			{
				counter++;
			}
		}

		class Test1Test : ITest
		{
			class Tester
			{
				public void Test(int loops, IMyNode node)
				{
					for (int i = 0; i < loops; ++i)
						node.Test();
				}
			}

			public void DoTest(int loops)
			{
				var tester = new Tester();
				tester.Test(loops, new MyNode());
			}
		}

		class Test2Test : ITest
		{
			class Tester
			{
				public void Test(int loops, MyNode node)
				{
					for (int i = 0; i < loops; ++i)
						node.Test();
				}
			}

			public void DoTest(int loops)
			{
				var tester = new Tester();
				tester.Test(loops, new MyNode());
			}
		}

		class Test3Test : ITest
		{
			class Tester<T> where T : IMyNode
			{
				public void Test(int loops, T node)
				{
					for (int i = 0; i < loops; ++i)
						node.Test();
				}
			}

			public void DoTest(int loops)
			{
				var tester = new Tester<MyNode>();
				tester.Test(loops, new MyNode());
			}
		}
	}
}
