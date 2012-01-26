using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PerfTest
{

	class EnumConvTestSuite : TestSuite
	{
		static MyEnum[] s_arr = new MyEnum[] { MyEnum.Eka, MyEnum.Toka, MyEnum.Kolmas, MyEnum.Neljas };
		static uint s_comp;

		public override void DoTests()
		{
			var arr = EnumConvTestSuite.s_arr;
			s_comp = 0;

			for (int i = 0; i < arr.Length; ++i)
				s_comp |= 1U << (int)arr[i];

			RunTest(new DirectConvTest());
			RunTest(new IConvertibleConvTest());
			RunTest(new ConvertConvTest());
			RunTest(new ObjConvTest());
			RunTest(new AbstrTest());
		}

		class DirectConvTest : ITest
		{
			public static uint Value;

			public void DoTest(int loops)
			{
				var arr = EnumConvTestSuite.s_arr;

				while (loops-- > 0)
				{
					for (int i = 0; i < arr.Length; ++i)
					{
						Value |= 1U << (int)arr[i];
					}
				}

				if (Value != s_comp)
					throw new Exception();
			}
		}

		class IConvertibleConvTest : ITest
		{
			public static uint Value;

			public void DoTest(int loops)
			{
				var arr = EnumConvTestSuite.s_arr;

				while (loops-- > 0)
				{
					for (int i = 0; i < arr.Length; ++i)
					{
						Value |= 1U << ((IConvertible)arr[i]).ToInt32(null);
					}
				}

				if (Value != s_comp)
					throw new Exception();
			}
		}

		class ConvertConvTest : ITest
		{
			public static uint Value;

			public void DoTest(int loops)
			{
				var arr = EnumConvTestSuite.s_arr;

				while (loops-- > 0)
				{
					for (int i = 0; i < arr.Length; ++i)
					{
						Value |= 1U << Convert.ToInt32(arr[i]);
					}
				}

				if (Value != s_comp)
					throw new Exception();
			}
		}

		class ObjConvTest : ITest
		{
			public static uint Value;

			public void DoTest(int loops)
			{
				var arr = EnumConvTestSuite.s_arr;

				while (loops-- > 0)
				{
					for (int i = 0; i < arr.Length; ++i)
					{
						Value |= 1U << (int)(object)arr[i];
					}
				}

				if (Value != s_comp)
					throw new Exception();
			}
		}

		sealed class AbstrTest : ItemFilter32<MyEnum>
		{

			protected override int ToInt(MyEnum value)
			{
				return (int)value;
			}

			protected override MyEnum ToT(int value)
			{
				return (MyEnum)value;
			}
		}

		abstract class ItemFilter32<TEnum> : ITest
		{
			public static uint Value;

			protected abstract int ToInt(TEnum value);
			protected abstract TEnum ToT(int value);

			public void DoTest(int loops)
			{
				var arr = EnumConvTestSuite.s_arr.Cast<TEnum>().ToArray();

				while (loops-- > 0)
				{
					for (int i = 0; i < arr.Length; ++i)
					{
						Value |= 1U << ToInt(arr[i]);
					}
				}

				if (Value != s_comp)
					throw new Exception();
			}
		}


		public enum MyEnum
		{
			None,
			Eka,
			Toka,
			Kolmas,
			Neljas,
			Viides,
		}
	}
}
