using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public struct IntCuboid
	{
		[DataMember(Name = "X")]
		readonly int m_x;
		[DataMember(Name = "Y")]
		readonly int m_y;
		[DataMember(Name = "Z")]
		readonly int m_z;
		[DataMember(Name = "Width")]
		readonly int m_width;
		[DataMember(Name = "Height")]
		readonly int m_height;
		[DataMember(Name = "Depth")]
		readonly int m_depth;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }
		public int Width { get { return m_width; } }
		public int Height { get { return m_height; } }
		public int Depth { get { return m_depth; } }

		public IntCuboid(int x, int y, int z, int width, int height, int depth)
		{
			m_x = x;
			m_y = y;
			m_z = z;
			m_width = width;
			m_height = height;
			m_depth = depth;
		}

		public IntCuboid(IntRect rect, int z)
			: this(rect.X, rect.Y, z, rect.Width, rect.Height, 1)
		{
		}

		public int X1 { get { return this.X; } }
		public int X2 { get { return this.X + this.Width; } }
		public int Y1 { get { return this.Y; } }
		public int Y2 { get { return this.Y + this.Height; } }
		public int Z1 { get { return this.Z; } }
		public int Z2 { get { return this.Z + this.Depth; } }

		public IntPoint3D Corner1
		{
			get { return new IntPoint3D(this.X, this.Y, this.Z); }
		}

		public IntPoint3D Corner2
		{
			get { return new IntPoint3D(this.X + this.Width, this.Y + this.Height, this.Z + this.Depth); }
		}

		public bool IsNull
		{
			get { return this.Width == 0 && this.Height == 0 && this.Depth == 0; }
		}

		public bool Contains(IntPoint3D l)
		{
			if (l.X < this.X || l.Y < this.Y || l.Z < this.Z ||
				l.X >= this.X + this.Width || l.Y >= this.Y + this.Height || l.Z >= this.Z + this.Depth)
				return false;
			else
				return true;
		}

		public IEnumerable<IntPoint3D> Range()
		{
			for (int z = this.Z; z < this.Z + this.Depth; ++z)
				for (int y = this.Y; y < this.Y + this.Height; ++y)
					for (int x = this.X; x < this.X + this.Width; ++x)
						yield return new IntPoint3D(x, y, z);
		}

		public override string ToString()
		{
			return String.Format("x={0},y={1},z={2},w={3},h={4},d={5}",
				this.X, this.Y, this.Z, this.Width, this.Height, this.Depth);
		}
	}
}
