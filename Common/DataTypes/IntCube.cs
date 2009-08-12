using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	/**
	 * Cube datatype with integer dimensions
	 */
	public struct IntCube
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int Z { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public int Depth{ get; set; }

		public IntCube(int x, int y, int z, int width, int height, int depth)
			: this()
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
			this.Width = width;
			this.Height = height;
			this.Depth = depth;
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

		public int Front
		{
			get { return Z; }
		}

		public int Back
		{
			get { return Z + Depth; }
		}

		public bool Contains(IntPoint3D l)
		{
			if (l.X < this.X || l.Y < this.Y || l.Z < this.Z ||
				l.X >= this.X + this.Width || l.Y >= this.Y + this.Height || l.Z >= this.Z + this.Depth)
				return false;
			else
				return true;
		}

		public override string ToString()
		{
			return String.Format("x={0},y={1},z={2},w={3},h={4},d={5}",
				X, Y, Z, Width, Height, Depth);
		}
	}
}
