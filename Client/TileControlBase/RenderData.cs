using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client.TileControl
{
	public class RenderData<T> : IRenderData where T : struct
	{
		ArrayGrid2D<T> m_grid;

		public RenderData()
		{
			m_grid = new ArrayGrid2D<T>(0, 0);
		}

		public IntSize Size
		{
			get { return m_grid.Size; }
			set { if (m_grid.Size != value) m_grid = new ArrayGrid2D<T>(value); }
		}

		public IntRect Bounds { get { return m_grid.Bounds; } }

		public ArrayGrid2D<T> ArrayGrid { get { return m_grid; } }

		public void Clear()
		{
			m_grid.Clear();
		}
	}
}
