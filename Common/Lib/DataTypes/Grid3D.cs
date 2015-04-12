using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Dwarrowdelf
{
	/// <summary>
	/// 3D grid made of Ts
	/// Coordinates offset by Origin
	/// </summary>
	public sealed class Grid3D<T>
	{
		ArrayGrid3D<T> m_grid;
		public IntVector3 Origin { get; set; }

		public Grid3D(int width, int height, int depth)
		{
			m_grid = new ArrayGrid3D<T>(width, height, depth);
			this.Origin = new IntVector3();
		}

		public Grid3D(int width, int height, int depth, int originX, int originY, int originZ)
			: this(width, height, depth)
		{
			this.Origin = new IntVector3(originX, originY, originZ);
		}

		public int Width { get { return m_grid.Width; } }
		public int Height { get { return m_grid.Height; } }
		public int Depth { get { return m_grid.Depth; } }

		public void Clear()
		{
			m_grid.Clear();
		}

		public T this[IntVector3 l]
		{
			get
			{
				l = l + this.Origin;
				return m_grid.Grid[l.Z, l.Y, l.X];
			}

			set
			{
				l = l + this.Origin;
				m_grid.Grid[l.Z, l.Y, l.X] = value;
			}
		}

		public T this[int x, int y, int z]
		{
			get { return m_grid.Grid[z, y, x]; }
			set { m_grid.Grid[z, y, x] = value; }
		}

		public IEnumerable<KeyValuePair<IntVector3, T>> GetIndexValueEnumerable()
		{
			for (int z = 0; z < this.Depth; z++)
			{
				for (int y = 0; y < this.Height; y++)
				{
					for (int x = 0; x < this.Width; x++)
					{
						yield return new KeyValuePair<IntVector3, T>(new IntVector3(x - Origin.X, y - Origin.Y, z - Origin.Z), m_grid.Grid[z, y, x]);
					}
				}
			}
		}
	}
}
