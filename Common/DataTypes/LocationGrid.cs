using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MyGame
{
	/**
	 * 2D grid made of <T>s
	 * Coordinates offset by Origin
	 */
	public class LocationGrid<T> : IEnumerable<KeyValuePair<IntPoint, T>>
	{
		T[,] m_grid;
		public IntPoint Origin { get; set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		public LocationGrid(int width, int height)
		{
			this.Width = width;
			this.Height = height;
			m_grid = new T[width, height];
			this.Origin = new IntPoint(0, 0);
		}

		public LocationGrid(int width, int height, int originX, int originY) : this(width, height)
		{
			this.Origin = new IntPoint(originX, originY);
		}

		public T this[IntPoint l]
		{
			get
			{
				l = l + this.Origin;
				return m_grid[l.X, l.Y];
			}

			set
			{
				l = l + this.Origin;
				m_grid[l.X, l.Y] = value;
			}
		}
		
		public T this[int x, int y]
		{
			get
			{
				return this[new IntPoint(x, y)];
			}

			set
			{
				this[new IntPoint(x, y)] = value;
			}
		}

		public IntRect Bounds
		{
			get 
			{
				return new IntRect(0 - this.Origin.X, 0 - this.Origin.Y,
					this.Width, this.Height);
			}
		}

		public IEnumerable<IntPoint> GetLocations()
		{
			for (int x = 0; x < this.Width; x++)
			{
				for (int y = 0; y < this.Height; y++)
				{
					yield return new IntPoint(x - Origin.X, y - Origin.Y);
				}
			}
		}

		#region IEnumerable<T> Members

		public IEnumerator<KeyValuePair<IntPoint, T>> GetEnumerator()
		{
			for (int x = 0; x < this.Width; x++)
			{
				for (int y = 0; y < this.Height; y++)
				{
					yield return new KeyValuePair<IntPoint, T>(
						new IntPoint(x - Origin.X, y - Origin.Y),
						m_grid[x, y]
						);
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
