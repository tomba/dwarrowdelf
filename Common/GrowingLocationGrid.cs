using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class GrowingLocationGrid<T>
	{
		LocationGrid<LocationGrid<T>> m_grid;

		public GrowingLocationGrid()
		{
			m_grid = new LocationGrid<LocationGrid<T>>(3, 3);
		}
	}
}
