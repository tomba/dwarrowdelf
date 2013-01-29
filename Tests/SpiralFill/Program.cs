using Dwarrowdelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpiralFill
{
	class Program
	{
		static void Main(string[] args)
		{
			Grid2D<int?> grid = new Grid2D<int?>(11, 11, 5, 5);

			var arr = IntPoint2.DiagonalSquareSpiral(new IntPoint2(0, 0), 5).ToArray();
			//var arr = IntPoint2.SquareSpiral(new IntPoint2(0, 0), 5).ToArray();

			Console.WriteLine("num values {0}", arr.Length);

			for (int i = 0; i < arr.Length; ++i)
			{
				var p = arr[i];

				if (grid[p].HasValue)
					throw new Exception();

				grid[p] = i;
			}


			var sb = new StringBuilder();

			for (int y = 0; y < grid.Height; ++y)
			{
				sb.Clear();

				for (int x = 0; x < grid.Width; ++x)
				{
					var v = grid[new IntPoint2(x, grid.Height - y - 1) - grid.Origin];

					if (v.HasValue)
						sb.AppendFormat("{0:000} ", v.Value);
					else
						sb.Append("... ");
				}

				Console.WriteLine(sb.ToString());
			}

			Console.ReadLine();
		}
	}
}
