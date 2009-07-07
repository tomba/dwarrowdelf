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
	public class LocationGrid3D<T> : IEnumerable<T>
	{
		T[,,] m_grid;
		public Location3D Origin { get; set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int Depth { get; private set; }

		public LocationGrid3D(int width, int height, int depth)
		{
			this.Width = width;
			this.Height = height;
			this.Depth = depth;
			m_grid = new T[width, height, depth];
			this.Origin = new Location3D(0, 0, 0);
		}

		public LocationGrid3D(int width, int height, int depth,
			int originX, int originY, int originZ) : this(width, height, depth)
		{
			this.Origin = new Location3D(originX, originY, originZ);
		}

		public T this[Location3D l]
		{
			get
			{
				l = l + this.Origin;
				return m_grid[l.X, l.Y, l.Z];
			}

			set
			{
				l = l + this.Origin;
				m_grid[l.X, l.Y, l.Z] = value;
			}
		}
		
		public T this[int x, int y, int z]
		{
			get { return this[new Location3D(x, y, z)]; }
			set { this[new Location3D(x, y, z)] = value; }
		}

		public IntCube Bounds
		{
			get 
			{
				return new IntCube(0 - this.Origin.X, 0 - this.Origin.Y, 0 - this.Origin.Z,
					this.Width, this.Height, this.Depth);
			}
		}

		public IEnumerable<Location3D> GetLocations()
		{
			for (int x = 0; x < this.Width; x++)
			{
				for (int y = 0; y < this.Height; y++)
				{
					for (int z = 0; z < this.Depth; z++)
					{
						yield return new Location3D(x - Origin.X, y - Origin.Y, z - Origin.Z);
					}
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
					for (int z = 0; z < this.Depth; z++)
					{
						yield return m_grid[x, y, z];
					}
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
