using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;

namespace AStarTest
{
	class RenderView
	{
		public RenderTileData[,] Grid { get; private set; }

		public int Width { get; private set; }
		public int Height { get; private set; }

		public void SetGridSize(IntSize size)
		{
			this.Grid = new RenderTileData[size.Height, size.Width];

			for (int y = 0; y < size.Height; ++y)
				for (int x = 0; x < size.Width; ++x)
					this.Grid[y, x] = new RenderTileData();

			this.Width = size.Width;
			this.Height = size.Height;
		}
	}
}
