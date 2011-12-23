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
		public IntSize Size { get; private set; }

		public T[,] Grid { get; private set; }

		public RenderData()
		{
			this.Grid = new T[0, 0];
		}

		public void SetSize(IntSize size)
		{
			if (this.Size == size)
				return;

			this.Grid = new T[size.Height, size.Width];
			this.Size = size;
		}

		public bool Contains(IntPoint p)
		{
			return p.X >= 0 && p.X < this.Width && p.Y >= 0 && p.Y < this.Height;
		}

		public void Clear()
		{
			Array.Clear(this.Grid, 0, this.Grid.Length);
		}
	}
}
