using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MemoryMappedLog
{
	class Program
	{
		static volatile bool s_exit = false;

		static void Main(string[] args)
		{
			var reader = new Thread(ReaderMain);
			var writer = new Thread(WriterMain);

			reader.Start();
			writer.Start();

			writer.Join();
			reader.Join();
		}

		static void ReaderMain()
		{
			int sinceIdx = 0;
			int tick = 0;

			while (s_exit == false)
			{
				var entries = MMLog.ReadNewEntries(sinceIdx, out sinceIdx);

				for (int i = 0; i < entries.Length; ++i)
				{
					if (entries[i].Tick != tick)
						throw new Exception();

					tick++;
				}
			}
		}

		static void WriterMain()
		{
			for (int i = 0; i < 1000; ++i)
			{
				MMLog.Append(i, "Client", "CMain", "Foo", String.Format("qwe {0}", i));
			}

			s_exit = true;
		}
	}
}
