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
			public double SW;
			public double SE;
			public double NW;
			public double NE;
		}

		sealed class Context
		{
			public Random Random;
			public ArrayGrid2D<double> Grid;
			public CornerData Corners;
			public double Range;
			public double H;
			public double Max;
			public double Min;
		}

		public static void Render(ArrayGrid2D<double> grid, CornerData corners, double range, double h, Random random,
			out double min, out double max)
		{
			if (grid.Width != grid.Height)
				throw new Exception();

			var ctx = new Context()
			{
				Random = random,
				Grid = grid,
				Corners = corners,
				Range = range,
				H = h,
				Min = Math.Min(Math.Min(Math.Min(corners.SW, corners.SE), corners.NE), corners.NW),
				Max = Math.Max(Math.Max(Math.Max(corners.SW, corners.SE), corners.NE), corners.NW),
			};

			grid[0, 0] = corners.SW;
			grid[grid.Width - 1, 0] = corners.SE;
			grid[grid.Width - 1, grid.Height - 1] = corners.NE;
			grid[0, grid.Height - 1] = corners.NW;

			HeightMap(ctx);

			min = ctx.Min;
			max = ctx.Max;
		}

		static double GetRandom(Context ctx, double range)
		{
			return ctx.Random.NextDouble() * range * 2 - range;
		}

		static void HeightMap(Context ctx)
		{
			var grid = ctx.Grid;

			for (int pass = 0; pass < MyMath.Log2(grid.Width); ++pass)
			{
				var parts = MyMath.Pow2(pass);
				var size = (grid.Width - 1) / parts;
				int half = size / 2;

				Debug.Assert(half != 0);

				for (int y = half; y < grid.Height; y += size)
				{
					for (int x = half; x < grid.Width; x += size)
					{
						var p = new IntVector2(x, y);
						Rectangle(ctx, p, half);
					}
				}

				bool odd = true;

				for (int y = 0; y < grid.Height; y += half)
				{
					for (int x = odd ? half : 0; x < grid.Width; x += size)
					{
						var p = new IntVector2(x, y);
						Diamond(ctx, p, half);
					}

					odd = !odd;
				}

				ctx.Range *= Math.Pow(2, -ctx.H);
			}
		}

		static void Rectangle(Context ctx, IntVector2 middle, int radius)
		{
			var grid = ctx.Grid;
			double v1, v2, v3, v4;
			IntVector2 p1, p2, p3, p4;

			p1 = middle.Offset(radius, radius);
			v1 = grid[p1];

			p2 = middle.Offset(-radius, radius);
			v2 = grid[p2];

			p3 = middle.Offset(radius, -radius);
			v3 = grid[p3];

			p4 = middle.Offset(-radius, -radius);
			v4 = grid[p4];

			var avg = (v1 + v2 + v3 + v4) / 4;
			var val = avg + GetRandom(ctx, ctx.Range);

			grid[middle] = val;

			if (val < ctx.Min)
				ctx.Min = val;
			if (val > ctx.Max)
				ctx.Max = val;
		}

		static void Diamond(Context ctx, IntVector2 middle, int radius)
		{
			double v1, v2, v3, v4;
			IntVector2 p1, p2, p3, p4;

			p1 = middle.Offset(0, -radius);
			v1 = GetGridValue(ctx, p1);

			p2 = middle.Offset(-radius, 0);
			v2 = GetGridValue(ctx, p2);

			p3 = middle.Offset(0, radius);
			v3 = GetGridValue(ctx, p3);

			p4 = middle.Offset(radius, 0);
			v4 = GetGridValue(ctx, p4);

			var avg = (v1 + v2 + v3 + v4) / 4;
			var val = avg + GetRandom(ctx, ctx.Range);

			ctx.Grid[middle] = val;

			if (val < ctx.Min)
				ctx.Min = val;
			if (val > ctx.Max)
				ctx.Max = val;
		}

		/// <summary>
		/// Get value from the grid, or if the point is outside the grid, a value averaged between two corners
		/// </summary>
		static double GetGridValue(Context ctx, IntVector2 p)
		{
			var grid = ctx.Grid;
			var corners = ctx.Corners;

			double v1, v2;
			double len;
			int pos;

			if (p.X < 0)
			{
				len = grid.Height;
				v1 = corners.SW;
				v2 = corners.NW;
				pos = p.Y;
			}
			else if (p.Y < 0)
			{
				len = grid.Width;
				v1 = corners.SW;
				v2 = corners.SE;
				pos = p.X;
			}
			else if (p.X >= grid.Width)
			{
				len = grid.Height;
				v1 = corners.SE;
				v2 = corners.NE;
				pos = p.Y;
			}
			else if (p.Y >= grid.Height)
			{
				len = grid.Width;
				v1 = corners.NW;
				v2 = corners.NE;
				pos = p.X;
			}
			else
			{
				return grid[p];
			}

			var m = (v2 - v1) / len;
			var h = m * pos + v1;

			return h;
		}
	}
}
