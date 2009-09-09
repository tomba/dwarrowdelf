using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MyGame
{
	/**
	 * 3D grid made of <T>s
	 */
	public class Grid3D<T> : Grid3DBase<T>
	{
		public Grid3D(int width, int height, int depth)
			: base(width, height, depth)
		{
		}

		public T this[IntPoint3D l]
		{
			get { return base.Grid[GetIndex(l)]; }
			set { base.Grid[GetIndex(l)] = value; }
		}
		
		public T this[int x, int y, int z]
		{
			get { return this[new IntPoint3D(x, y, z)]; }
			set { this[new IntPoint3D(x, y, z)] = value; }
		}

		public IntCube Bounds
		{
			get 
			{
				return new IntCube(0, 0, 0, this.Width, this.Height, this.Depth);
			}
		}
	}
}
