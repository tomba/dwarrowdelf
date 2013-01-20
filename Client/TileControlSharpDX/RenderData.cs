using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class RenderData<T> : IRenderData where T : struct
	{
		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public IntSize2 Size { get; private set; }
		public bool Invalid { get; set; }

		public T[] Grid { get; private set; }

		public RenderData()
		{
			this.Grid = new T[0];
		}

		public void SetMaxSize(IntSize2 size)
		{
			int len = size.Width * size.Height;

			if (len == this.Grid.Length)
				return;

			this.Grid = new T[size.Width * size.Height];
			this.Invalid = true;
		}

		public void SetSize(IntSize2 size)
		{
			if (this.Size == size)
				return;

			int len = size.Width * size.Height;
			if (len > this.Grid.Length)
				throw new Exception();

			this.Size = size;
			this.Invalid = true;
		}

		public int GetIdx(int x, int y)
		{
			return x + y * this.Size.Width;
		}

		public int GetIdx(IntPoint2 p)
		{
			return p.X + p.Y * this.Size.Width;
		}

		public bool Contains(IntPoint2 p)
		{
			return p.X >= 0 && p.X < this.Width && p.Y >= 0 && p.Y < this.Height;
		}

		public void Clear()
		{
			Array.Clear(this.Grid, 0, this.Size.Width * this.Size.Height);
		}
	}
}
