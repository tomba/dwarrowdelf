using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Dwarrowdelf;
using System.Windows;

namespace AStarTest
{
	struct RenderTileData
	{
		public Brush Brush;
		public int G;
		public int H;
		public Direction From;
		public int Weight;
		public Stairs Stairs;

		public RenderTileData(Brush bg)
		{
			this.Brush = bg;
			this.G = 0;
			this.H = 0;
			this.From = Direction.None;
			this.Weight = 0;
			this.Stairs = Stairs.None;
		}
	}

	class RenderData
	{
		public RenderTileData[,] Grid { get; private set; }

		public int Width { get; private set; }
		public int Height { get; private set; }

		public void SetGridSize(IntSize2 size)
		{
			if (size.Width == this.Width && size.Height == this.Height)
				return;

			this.Grid = new RenderTileData[size.Height, size.Width];

			this.Width = size.Width;
			this.Height = size.Height;
		}
	}
}
