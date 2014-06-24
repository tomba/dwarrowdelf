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
	public sealed class ArrayGrid2D<T> : IEnumerable<T>
	{
		T[,] m_grid;

		public int Width { get { return m_grid.GetLength(1); } }
		public int Height { get { return m_grid.GetLength(0); } }
		public IntSize2 Size { get { return new IntSize2(this.Width, this.Height); } }

		public T[,] Grid { get { return m_grid; } }

		public ArrayGrid2D(int width, int height)
		{
			m_grid = new T[height, width];
		}

		public ArrayGrid2D(IntSize2 size)
			: this(size.Width, size.Height)
		{
		}

		public T this[IntVector2 l]
		{
			get { return m_grid[l.Y, l.X]; }
			set { m_grid[l.Y, l.X] = value; }
		}

		public T this[int x, int y]
		{
			get { return m_grid[y, x]; }
			set { m_grid[y, x] = value; }
		}

		public IntGrid2 Bounds
		{
			get { return new IntGrid2(0, 0, this.Width, this.Height); }
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

		public void ForEach(Func<T, T> action)
		{
			for (int y = 0; y < this.Height; y++)
			{
				for (int x = 0; x < this.Width; x++)
				{
					m_grid[y, x] = action(m_grid[y, x]);
				}
			}
		}

		public IEnumerable<KeyValuePair<IntVector2, T>> GetIndexValueEnumerable()
		{
			for (int y = 0; y < this.Height; y++)
			{
				for (int x = 0; x < this.Width; x++)
				{
					yield return new KeyValuePair<IntVector2, T>(new IntVector2(x, y), m_grid[y, x]);
				}
			}
		}

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			for (int y = 0; y < this.Height; y++)
			{
				for (int x = 0; x < this.Width; x++)
				{
					yield return m_grid[y, x];
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
