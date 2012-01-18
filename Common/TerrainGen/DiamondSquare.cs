using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.TerrainGen
{
	public static class DiamondSquare
	{
		public class CornerData
		{
			public double NW;
			public double NE;
			public double SW;
			public double SE;
		}

		sealed class Context
		{
			public Random Random;
			public ArrayGrid2D<double> Grid;
			public CornerData Corners;
			public double Range;
			public double H;
		}

		public static void Render(ArrayGrid2D<double> grid, CornerData corners, double range, double h, int randomSeed)
		{
			if (grid.Width != grid.Height)
				throw new Exception();

			var ctx = new Context()
			{
				Random = new Random(randomSeed),
				Grid = grid,
				Corners = corners,
				Range = range,
				H = h,
			};

			grid[0, 0] = corners.NW;
			grid[grid.Width - 1, 0] = corners.NE;
			grid[grid.Width - 1, grid.Height - 1] = corners.SE;
			grid[0, grid.Height - 1] = corners.SW;

			HeightMap(ctx);
		}

		static double GetRandom(Context ctx, double range)
		{
			return ctx.Random.NextDouble() * range * 2 - range;
		}

		static void HeightMap(Context ctx)
		{
			var grid = ctx.Grid;
			var corners = ctx.Corners;

			for (int pass = 0; pass < (int)Math.Log(grid.Width, 2); ++pass)
			{
				var parts = (int)Math.Pow(2, pass);
				var size = (grid.Width - 1) / parts;

				if (size == 0)
					throw new Exception();

				for (int y = 0; y < parts; ++y)
				{
					for (int x = 0; x < parts; ++x)
					{
						var rect = new IntRect(size * x, size * y, size, size);
						int radius = rect.Width / 2;

						if (radius == 0)
							throw new Exception();

						var middle = rect.X1Y1 + new IntVector2(radius, radius);

						Rectangle(ctx, middle, radius);

						var mxy1 = new IntPoint2(middle.X, rect.Y1);
						var mxy2 = new IntPoint2(middle.X, rect.Y2);
						var x1my = new IntPoint2(rect.X1, middle.Y);
						var x2my = new IntPoint2(rect.X2, middle.Y);

						Diamond(ctx, x1my, radius);
						Diamond(ctx, x2my, radius);
						Diamond(ctx, mxy1, radius);
						Diamond(ctx, mxy2, radius);
					}
				}

				ctx.Range *= Math.Pow(2, -ctx.H);
			}
		}

		static void Rectangle(Context ctx, IntPoint2 middle, int radius)
		{
			var grid = ctx.Grid;
			double v1, v2, v3, v4;
			IntPoint2 p1, p2, p3, p4;

			p1 = middle.Offset(radius, radius);
			v1 = grid[p1];

			p2 = middle.Offset(-radius, radius);
			v2 = grid[p2];

			p3 = middle.Offset(radius, -radius);
			v3 = grid[p3];

			p4 = middle.Offset(-radius, -radius);
			v4 = grid[p4];

			var avg = (v1 + v2 + v3 + v4) / 4;
			grid[middle] = avg + GetRandom(ctx, ctx.Range);
		}

		static void Diamond(Context ctx, IntPoint2 middle, int radius)
		{
			double v1, v2, v3, v4;
			IntPoint2 p1, p2, p3, p4;

			p1 = middle.Offset(0, -radius);
			v1 = GetGridValue(ctx, p1);

			p2 = middle.Offset(-radius, 0);
			v2 = GetGridValue(ctx, p2);

			p3 = middle.Offset(0, radius);
			v3 = GetGridValue(ctx, p3);

			p4 = middle.Offset(radius, 0);
			v4 = GetGridValue(ctx, p4);

			var avg = (v1 + v2 + v3 + v4) / 4;
			ctx.Grid[middle] = avg + GetRandom(ctx, ctx.Range);
		}

		/// <summary>
		/// Get value from the grid, or if the point is outside the grid, a value averaged between two corners
		/// </summary>
		static double GetGridValue(Context ctx, IntPoint2 p)
		{
			var grid = ctx.Grid;
			var corners = ctx.Corners;

			double y1, y2;
			double x1, x2;
			double x, y;

			if (p.X < 0)
			{
				x1 = 0;
				y1 = corners.NW;

				x2 = grid.Height;
				y2 = corners.SW;

				x = p.Y;
			}
			else if (p.Y < 0)
			{
				x1 = 0;
				y1 = corners.NW;

				x2 = grid.Width;
				y2 = corners.NE;

				x = p.X;
			}
			else if (p.X >= grid.Width)
			{
				x1 = 0;
				y1 = corners.NE;

				x2 = grid.Height;
				y2 = corners.SE;

				x = p.Y;
			}
			else if (p.Y >= grid.Height)
			{
				x1 = 0;
				y1 = corners.SE;

				x2 = grid.Width;
				y2 = corners.SW;

				x = p.X;
			}
			else
			{
				return grid[p];
			}

			var m = (y1 - y2) / (x1 - x2);

			y = m * x + y1;

			return y;
		}
	}
}
