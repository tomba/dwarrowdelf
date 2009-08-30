using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MyGame
{
	public class Grid2DBase<T>
	{
		public int Width { get; private set; }
		public int Height { get; private set; }

		public Grid2DBase(int width, int height)
		{
			this.Width = width;
			this.Height = height;
			this.Grid = new T[width * height];
		}

		protected T[] Grid { get; private set; }

		protected int GetIndex(IntPoint p)
		{
			return p.Y * this.Width + p.X;
		}
	}

	/**
	 * 2D grid made of <T>s
	 * Coordinates offset by Origin
	 */
	public class LocationGrid<T> : Grid2DBase<T>, IEnumerable<KeyValuePair<IntPoint, T>>
	{
		public IntVector Origin { get; set; }

		public LocationGrid(int width, int height) : base(width, height)
		{
			this.Origin = new IntVector(0, 0);
		}

		public LocationGrid(int width, int height, int originX, int originY) : this(width, height)
		{
			this.Origin = new IntVector(originX, originY);
		}

		public T this[IntPoint l]
		{
			get
			{
				l = l + this.Origin;
				return base.Grid[GetIndex(l)];
			}

			set
			{
				l = l + this.Origin;
				base.Grid[GetIndex(l)] = value;
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
			for (int y = 0; y < this.Height; y++)
			{
				for (int x = 0; x < this.Width; x++)
				{
					yield return new KeyValuePair<IntPoint, T>(
						new IntPoint(x - Origin.X, y - Origin.Y),
						base.Grid[GetIndex(new IntPoint(x, y))]
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
