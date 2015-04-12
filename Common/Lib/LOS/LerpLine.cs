using System;
using System.Collections.Generic;

namespace Dwarrowdelf
{
	public static class LerpLine
	{
		/// <summary>
		/// Line using linear interpolation
		/// </summary>
		public static IEnumerable<IntVector2> PlotLine(IntVector2 p0, IntVector2 p1)
		{
			const int MUL = 1 << 16;

			IntVector2 v = p1 - p0;

			int N = Math.Max(Math.Abs(v.X), Math.Abs(v.Y));

			int dx, dy;

			if (N == 0)
			{
				dx = dy = 0;
			}
			else
			{
				dx = (v.X * MUL) / N;
				dy = (v.Y * MUL) / N;
			}

			int x = 0;
			int y = 0;

			for (int step = 0; step <= N; step++)
			{
				var p = new IntVector2(p0.X + MyMath.DivRoundNearest(x, MUL), p0.Y + MyMath.DivRoundNearest(y, MUL));

				yield return p;

				x += dx;
				y += dy;
			}
		}

		/// <summary>
		/// Line using linear interpolation
		/// </summary>
		public static IEnumerable<IntVector3> PlotLine3(IntVector3 p0, IntVector3 p1)
		{
			const int MUL = 1 << 16;

			IntVector3 v = p1 - p0;

			int N = MyMath.Max(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));

			int dx, dy, dz;

			if (N == 0)
			{
				dx = dy = dz = 0;
			}
			else
			{
				dx = (v.X * MUL) / N;
				dy = (v.Y * MUL) / N;
				dz = (v.Z * MUL) / N;
			}

			int x = 0;
			int y = 0;
			int z = 0;

			for (int step = 0; step <= N; step++)
			{
				var p = new IntVector3(p0.X + MyMath.DivRoundNearest(x, MUL),
					p0.Y + MyMath.DivRoundNearest(y, MUL),
					p0.Z + MyMath.DivRoundNearest(z, MUL));

				yield return p;

				x += dx;
				y += dy;
				z += dz;
			}
		}
	}
}
