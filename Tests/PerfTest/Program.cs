using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PerfTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var tests = new TestSuite[] 
			{
				new GenericIFaceAccessTestSuite(),
				//new EnumConvTestSuite(),
				//new IntPointTestSuite(),
				//new ArrayAccessTestSuite(),
				//new StructAccessTestSuite(),
				//new SkillMapTestSuite(),
				//new LocalMethodCallTestSuite(),
				//new RemoteMethodCallTestSuite(),
			};

			foreach (var test in tests)
				RunSuite(test);

			//Console.WriteLine("Done. Press enter to quit.");
			//Console.ReadLine();
		}

		static void RunSuite(TestSuite suite)
		{
			Console.WriteLine("Running suite {0}", suite.GetType().Name);
			suite.DoTests();
		}
	}


	interface ITest
	{
		void DoTest(int loops);
	}

	abstract class TestSuite
	{
		public abstract void DoTests();

		protected static void RunTest(ITest test)
		{
			int loops = Calibrate(test, TimeSpan.FromMilliseconds(1000));

			var sw = new Stopwatch();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			var c1 = GC.CollectionCount(0);
			var c2 = GC.CollectionCount(1);
			var c3 = GC.CollectionCount(2);

			sw.Start();
			test.DoTest(loops);
			sw.Stop();

			c1 = GC.CollectionCount(0) - c1;
			c2 = GC.CollectionCount(1) - c2;
			c3 = GC.CollectionCount(2) - c3;

			var lps = (int)(loops / (sw.ElapsedMilliseconds / 1000.0));

			Console.WriteLine("{0,-40} {1,20} loops/s, collections {2}/{3}/{4}",
				test.GetType().Name, lps,
				c1, c2, c3);
		}

		static int Calibrate(ITest test, TimeSpan time)
		{
			int loops = 4;
			var sw = new Stopwatch();

			test.DoTest(1);

			while (true)
			{
				sw.Restart();
				test.DoTest(loops);
				sw.Stop();

				if (sw.ElapsedMilliseconds > 250)
					break;

				checked
				{
					if (sw.ElapsedMilliseconds <= 1)
						loops *= 128;
					else if (sw.ElapsedMilliseconds <= 10)
						loops *= 16;
					else
						loops *= 2;
				}
			}

			var lps = loops / (sw.ElapsedMilliseconds / 1000.0);

			var totalLoops = (int)(lps * time.TotalSeconds);

			if (totalLoops == 0)
				totalLoops = 1;

			return totalLoops;
		}
	}
}
