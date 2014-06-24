using System;

namespace Dwarrowdelf
{
	public static class Bresenhams
	{
		static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }

		/// <summary>
		/// Plot the line from src to dst
		/// </summary>
		/// <param name="plot">The plotting function (if this returns false, the algorithm stops early)</param>
		public static void PlotLine(IntVector2 src, IntVector2 dst, Func<IntVector2, bool> plot)
		{
			int x0 = src.X;
			int y0 = src.Y;
			int x1 = dst.X;
			int y1 = dst.Y;

			bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

			if (steep)
			{
				Swap<int>(ref x0, ref y0);
				Swap<int>(ref x1, ref y1);
			}

			if (x0 > x1)
			{
				Swap<int>(ref x0, ref x1);
				Swap<int>(ref y0, ref y1);
			}

			int dX = x1 - x0;
			int dY = Math.Abs(y1 - y0);
			int err = dX / 2;
			int ystep = y0 < y1 ? 1 : -1;
			int y = y0;

			for (int x = x0; x <= x1; ++x)
			{
				IntVector2 p = steep ? new IntVector2(y, x) : new IntVector2(x, y);

				if (!plot(p))
					return;

				err = err - dY;
				if (err < 0)
				{
					y += ystep;
					err += dX;
				}
			}
		}

		/// <summary>
		/// Plot the line from src to dst
		/// </summary>
		/// <param name="plot">The plotting function (if this returns false, the algorithm stops early)</param>
		public static void PlotLine3(IntVector3 src, IntVector3 dst, Func<IntVector3, bool> plot)
		{
			int x0 = src.X;
			int y0 = src.Y;
			int z0 = src.Z;
			int x1 = dst.X;
			int y1 = dst.Y;
			int z1 = dst.Z;

			bool steepXY = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
			if (steepXY)
			{
				Swap(ref x0, ref y0);
				Swap(ref x1, ref y1);
			}

			bool steepXZ = Math.Abs(z1 - z0) > Math.Abs(x1 - x0);
			if (steepXZ)
			{
				Swap(ref x0, ref z0);
				Swap(ref x1, ref z1);
			}

			int deltaX = Math.Abs(x1 - x0);
			int deltaY = Math.Abs(y1 - y0);
			int deltaZ = Math.Abs(z1 - z0);

			int errorXY = deltaX / 2;
			int errorXZ = deltaX / 2;

			int stepX = (x0 > x1) ? -1 : 1;
			int stepY = (y0 > y1) ? -1 : 1;
			int stepZ = (z0 > z1) ? -1 : 1;

			int y = y0;
			int z = z0;

			for (int x = x0; x <= x1; x += stepX)
			{
				int tx = x, ty = y, tz = z;

				if (steepXZ)
					Swap(ref tx, ref tz);
				if (steepXY)
					Swap(ref tx, ref ty);

				if (plot(new IntVector3(tx, ty, tz)) == false)
					return;

				errorXY -= deltaY;
				errorXZ -= deltaZ;

				if (errorXY < 0)
				{
					y += stepY;
					errorXY += deltaX;
				}

				if (errorXZ < 0)
				{
					z += stepZ;
					errorXZ += deltaX;
				}
			}
		}
	}
}
