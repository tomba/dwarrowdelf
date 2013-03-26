using Dwarrowdelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashCodeTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var map = new Dictionary<int, uint>();
			int tot = 0;
			foreach (var p in new IntGrid3(-100, -100, -100, 400, 200, 200).Range())
			{
				var h = p.GetHashCode();

				uint c;
				if (map.TryGetValue(h, out c))
					map[h] = c + 1;
				else
					map[h] = 1;

				tot++;
			}

			Console.WriteLine("tot {0}, distinct {1}", tot, map.Count);
		}
	}
}
