using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace PerfTest
{
	class StructAccessTestSuite : TestSuite
	{
		public override void DoTests()
		{
			RunTest(new ReferenceAccess());
			RunTest(new StructAccess());
			RunTest(new StructStructAccess());
		}

		class ReferenceAccess : ITest
		{
			public int A;
			public int B;
			public int C;
			public int D;

			public int res;

			public void DoTest(int loops)
			{
				for (int i = 0; i < loops; ++i)
				{
					A += 1;
					B += 2;
					C += 3;
					D += 4;

					A += 1;
					B += 2;
					C += 3;
					D += 4;

					A += 1;
					B += 2;
					C += 3;
					D += 4;

					A += 1;
					B += 2;
					C += 3;
					D += 4;

					A += 1;
					B += 2;
					C += 3;
					D += 4;
				}

				res = A + B + C + D;
			}
		}

		class StructAccess : ITest
		{
			struct StructA
			{
				public int A;
				public int B;
				public int C;
				public int D;
			}

			public int res;

			StructA m_struct;

			public void DoTest(int loops)
			{
				for (int i = 0; i < loops; ++i)
				{
					m_struct.A += 1;
					m_struct.B += 2;
					m_struct.C += 3;
					m_struct.D += 4;

					m_struct.A += 1;
					m_struct.B += 2;
					m_struct.C += 3;
					m_struct.D += 4;

					m_struct.A += 1;
					m_struct.B += 2;
					m_struct.C += 3;
					m_struct.D += 4;

					m_struct.A += 1;
					m_struct.B += 2;
					m_struct.C += 3;
					m_struct.D += 4;

					m_struct.A += 1;
					m_struct.B += 2;
					m_struct.C += 3;
					m_struct.D += 4;
				}

				res = m_struct.A + m_struct.B + m_struct.C + m_struct.D;
			}
		}

		class StructStructAccess : ITest
		{
			struct StructBase
			{
				public StructA Str;
			}

			struct StructA
			{
				public int A;
				public int B;
				public int C;
				public int D;
			}

			public int res;

			StructBase m_struct;

			public void DoTest(int loops)
			{
				for (int i = 0; i < loops; ++i)
				{
					m_struct.Str.A += 1;
					m_struct.Str.B += 2;
					m_struct.Str.C += 3;
					m_struct.Str.D += 4;

					m_struct.Str.A += 1;
					m_struct.Str.B += 2;
					m_struct.Str.C += 3;
					m_struct.Str.D += 4;

					m_struct.Str.A += 1;
					m_struct.Str.B += 2;
					m_struct.Str.C += 3;
					m_struct.Str.D += 4;

					m_struct.Str.A += 1;
					m_struct.Str.B += 2;
					m_struct.Str.C += 3;
					m_struct.Str.D += 4;

					m_struct.Str.A += 1;
					m_struct.Str.B += 2;
					m_struct.Str.C += 3;
					m_struct.Str.D += 4;
				}

				res = m_struct.Str.A + m_struct.Str.B + m_struct.Str.C + m_struct.Str.D;
			}
		}


	}
}

