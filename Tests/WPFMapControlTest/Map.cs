using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGame;

namespace WPFMapControlTest
{
	class Map
	{
		public byte[,] MapArray { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		public Map(int width, int height)
		{
			MapArray = new byte[height, width];
			Width = width;
			Height = height;
		}

		public IntRect Bounds { get { return new IntRect(0, 0, Width, Height); } }
	}
}
