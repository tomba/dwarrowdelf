using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public struct IntRect
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }

		public IntRect(int x, int y, int width, int height)
			: this()
		{
			this.X = x;
			this.Y = y;
			this.Width = width;
			this.Height = height;
		}

		public int Left
		{
			get { return X; }
		}

		public int Right
		{
			get { return X + Width; }
		}

		public int Top
		{
			get { return Y; }
		}

		public int Bottom
		{
			get { return Y + Height; }
		}

		public bool Contains(Location l)
		{
			if (l.X < this.X || l.Y < this.Y || l.X >= this.X + this.Width || l.Y >= this.Y + this.Height)
				return false;
			else
				return true;
		}

		public override string ToString()
		{
			return ("{X=" + this.X + ",Y=" + this.Y + ",Width=" + this.Width + ",Height=" + this.Height + "}");
		}
	}
}
