using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public struct IntRect
	{
		[DataMember]
		public int X { get; set; }
		[DataMember]
		public int Y { get; set; }
		[DataMember]
		public int Width { get; set; }
		[DataMember]
		public int Height { get; set; }

		public IntRect(int x, int y, int width, int height)
			: this()
		{
			this.X = x;
			this.Y = y;
			this.Width = width;
			this.Height = height;
		}

		public IntRect(IntPoint point1, IntPoint point2) : this()
		{
			this.X = Math.Min(point1.X, point2.X);
			this.Y = Math.Min(point1.Y, point2.Y);
			this.Width = Math.Max((Math.Max(point1.X, point2.X) - this.X), 0);
			this.Height = Math.Max((Math.Max(point1.Y, point2.Y) - this.Y), 0);
		}

		public IntRect(IntPoint point, IntSize size) : this()
		{
			this.X = point.X;
			this.Y = point.Y;
			this.Width = size.Width;
			this.Height = size.Height;
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

		public IntPoint TopLeft
		{
			get { return new IntPoint(this.X, this.Y); }
		}

		public IntPoint BottomRight
		{
			get { return new IntPoint(this.X + this.Width, this.Y + this.Height); }
		}

		public bool Contains(IntPoint l)
		{
			if (l.X < this.X || l.Y < this.Y || l.X >= this.X + this.Width || l.Y >= this.Y + this.Height)
				return false;
			else
				return true;
		}

		public IEnumerable<IntPoint> Range()
		{
			for (int y = this.Y; y < this.Y + this.Height; ++y)
				for (int x = this.X; x < this.X + this.Width; ++x)
					yield return new IntPoint(x, y);
		}

		public override string ToString()
		{
			return ("{X=" + this.X + ",Y=" + this.Y + ",Width=" + this.Width + ",Height=" + this.Height + "}");
		}
	}
}
