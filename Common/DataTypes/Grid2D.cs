using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Dwarrowdelf
{
	/// <summary>
	/// 2D grid made of Ts
	/// Coordinates offset by Origin
	/// </summary>
	public sealed class Grid2D<T> : IEnumerable<T>
	{
		ArrayGrid2D<T> m_grid;
		public IntVector2 Origin { get; set; }

		public Grid2D(int width, int height)
		{
			m_grid = new ArrayGrid2D<T>(width, height);
			this.Origin = new IntVector2(0, 0);
		}

		public Grid2D(int width, int height, int originX, int originY)
			: this(width, height)
		{
			this.Origin = new IntVector2(originX, originY);
		}

		public int Width { get { return m_grid.Width; } }
		public int Height { get { return m_grid.Height; } }

		public T this[IntPoint2 l]
		{
			get
			{
				l = l + this.Origin;
				return m_grid.Grid[l.Y, l.X];
			}

			set
			{
				l = l + this.Origin;
				m_grid.Grid[l.Y, l.X] = value;
			}
		}

		public T this[int x, int y]
		{
			get { return this[new IntPoint2(x, y)]; }
			set { this[new IntPoint2(x, y)] = value; }
		}

		public IntGrid2 Bounds
		{
			get { return new IntGrid2(0 - this.Origin.X, 0 - this.Origin.Y, this.Width, this.Height); }
		}

		public IEnumerable<KeyValuePair<IntPoint2, T>> GetIndexValueEnumerable()
		{
			for (int y = 0; y < this.Height; y++)
			{
				for (int x = 0; x < this.Width; x++)
				{
					yield return new KeyValuePair<IntPoint2, T>(new IntPoint2(x - Origin.X, y - Origin.Y), m_grid.Grid[y, x]);
				}
			}
		}

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			return m_grid.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
