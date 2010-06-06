using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace NetSerializer
{
	abstract class Test
	{
		public void Run(int loops)
		{
			var sw = new Stopwatch();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			RunOverride(2);

			GC.Collect();
			GC.WaitForPendingFinalizers();

			var c1 = GC.CollectionCount(0);
			var c2 = GC.CollectionCount(1);
			var c3 = GC.CollectionCount(2);

			sw.Start();
			RunOverride(loops);
			sw.Stop();

			c1 = GC.CollectionCount(0) - c1;
			c2 = GC.CollectionCount(1) - c2;
			c3 = GC.CollectionCount(2) - c3;

			GC.Collect();

			Console.WriteLine("{0}: {1} ms, {2} ticks, collections {3}/{4}/{5}",
				GetType().Name, sw.ElapsedMilliseconds, sw.ElapsedTicks,
				c1, c2, c3);
		}

		protected abstract void RunOverride(int loops);
	}
}
