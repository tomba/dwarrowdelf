using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MyGame
{
	public class LocationGrid<T> : IEnumerable<T>
	{
		T[,] m_grid;
		public Location Origin { get; set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		public LocationGrid(int width, int height)
		{
			this.Width = width;
			this.Height = height;
			m_grid = new T[width, height];
		}

		public LocationGrid(int width, int height, int originX, int originY) : this(width, height)
		{
			this.Origin = new Location(originX, originY);
		}

		public T this[Location l]
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
				return this[new Location(x, y)];
			}

			set
			{
				this[new Location(x, y)] = value;
			}
		}

		public IEnumerable<Location> GetLocations()
		{
			for (int x = 0; x < this.Width; x++)
			{
				for (int y = 0; y < this.Height; y++)
				{
					yield return new Location(x, y);
				}
			}
		}

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			for (int x = 0; x < this.Width; x++)
			{
				for (int y = 0; y < this.Height; y++)
				{
					yield return m_grid[x, y];
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
