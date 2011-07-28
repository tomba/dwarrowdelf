using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace PerfTest
{
	class LocalMethodCallTestSuite : TestSuite
	{
		public override void DoTests()
		{
			RunTest(new DirectCall());
			RunTest(new DirectCallNoInlining());
			RunTest(new InterfaceCall());
			RunTest(new OverrideCall());
			RunTest(new VirtualCall());
		}

		class DirectCall : ITest
		{
			public int Value;

			public void DoTest(int loops)
			{
				int v = 0;
				for (int i = 0; i < loops; ++i)
				{
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
				}

				this.Value = v;
			}

			public int Test(int value)
			{
				return value++;
			}
		}

		class DirectCallNoInlining : ITest
		{
			public int Value;

			public void DoTest(int loops)
			{
				int v = 0;
				for (int i = 0; i < loops; ++i)
				{
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
				}

				this.Value = v;
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public int Test(int value)
			{
				return value++;
			}
		}

		interface IMyInterface
		{
			int Test(int value);
		}

		class InterfaceCall : ITest, IMyInterface
		{
			public int Value;

			public void DoTest(int loops)
			{
				int v = 0;
				for (int i = 0; i < loops; ++i)
				{
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
				}

				this.Value = v;
			}

			public int Test(int value)
			{
				return value++;
			}
		}

		abstract class BaseAbstractClass
		{
			public abstract int Test(int value);
		}

		class OverrideCall : BaseAbstractClass, ITest
		{
			public int Value;

			public void DoTest(int loops)
			{
				int v = 0;
				for (int i = 0; i < loops; ++i)
				{
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
				}

				this.Value = v;
			}

			public override int Test(int value)
			{
				return value++;
			}
		}

		abstract class BaseVirtualClass
		{
			public virtual int Test(int value) { return value; }
		}

		class VirtualCall : BaseVirtualClass, ITest
		{
			public int Value;

			public void DoTest(int loops)
			{
				int v = 0;
				for (int i = 0; i < loops; ++i)
				{
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
					v = Test(v);
				}

				this.Value = v;
			}

			public override int Test(int value)
			{
				return value++;
			}
		}
	}
}

