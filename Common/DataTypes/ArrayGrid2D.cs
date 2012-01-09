using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Dwarrowdelf
{
	/// <summary>
	/// 2D grid made of Ts
	/// </summary>
	public sealed class ArrayGrid2D<T> : IEnumerable<KeyValuePair<IntPoint, T>>
	{
		T[,] m_grid;

		public int Width { get { return m_grid.GetLength(1); } }
		public int Height { get { return m_grid.GetLength(0); } }
		public IntSize Size { get { return new IntSize(this.Width, this.Height); } }

		public T[,] Grid { get { return m_grid; } }

		public ArrayGrid2D(int width, int height)
		{
			m_grid = new T[height, width];
		}

		public ArrayGrid2D(IntSize size)
			: this(size.Width, size.Height)
		{
		}

		public T this[IntPoint l]
		{
			get { return m_grid[l.Y, l.X]; }
			set { m_grid[l.Y, l.X] = value; }
		}

		public T this[int x, int y]
		{
			get { return m_grid[y, x]; }
			set { m_grid[y, x] = value; }
		}

		public IntRect Bounds
		{
			get { return new IntRect(0, 0, this.Width, this.Height); }
		}

		public void Fill(T data)
		{
			for (int y = 0; y < this.Height; y++)
			{
				for (int x = 0; x < this.Width; x++)
				{
					m_grid[y, x] = data;
				}
			}
		}

		public void Clear()
		{
			Array.Clear(m_grid, 0, m_grid.Length);
		}

		public IEnumerable<IntPoint> GetLocations()
		{
			for (int x = 0; x < this.Width; x++)
			{
				for (int y = 0; y < this.Height; y++)
				{
					yield return new IntPoint(x, y);
				}
			}
		}

		#region IEnumerable<T> Members

		public IEnumerator<KeyValuePair<IntPoint, T>> GetEnumerator()
		{
			for (int y = 0; y < this.Height; y++)
			{
				for (int x = 0; x < this.Width; x++)
				{
					yield return new KeyValuePair<IntPoint, T>(new IntPoint(x, y), m_grid[y, x]);
				}
			}
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
