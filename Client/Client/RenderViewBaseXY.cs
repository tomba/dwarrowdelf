using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf.Client.TileControl;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	abstract class RenderViewBaseXY<T> : RenderViewBase<T> where T : struct
	{
		IntGrid3 m_bounds;

		/* How many levels to show */
		protected const int MAXLEVEL = 4;

		protected RenderViewBaseXY(DataGrid2D<T> renderData)
			: base(renderData)
		{
		}

		protected override void OnCenterPosChanged(IntVector3 diff)
		{
			if (!m_renderData.Invalid)
			{
				if (diff.Z != 0)
					m_renderData.Invalid = true;
				else
					ScrollTiles(new IntVector2(diff.X, diff.Y));
			}

			var cp = CenterPos;
			var s = m_renderData.Size;
			m_bounds = new IntGrid3(new IntPoint3(cp.X - s.Width / 2, cp.Y - s.Height / 2, cp.Z - MAXLEVEL + 1),
				new IntSize3(s, MAXLEVEL));
		}

		protected override void OnSizeChanged()
		{
			var cp = CenterPos;
			var s = m_renderData.Size;
			m_bounds = new IntGrid3(new IntPoint3(cp.X - s.Width / 2, cp.Y - s.Height / 2, cp.Z - MAXLEVEL + 1),
				new IntSize3(s, MAXLEVEL));
		}

		protected IntPoint3 RenderDataLocationToMapLocation(int x, int y)
		{
			int sx = x + m_bounds.X1;
			int sy = y + m_bounds.Y1;

			return new IntPoint3(sx, sy, m_centerPos.Z);
		}

		protected IntPoint2 MapLocationToRenderDataLocation(IntPoint3 p)
		{
			var x = p.X - m_bounds.X1;
			var y = p.Y - m_bounds.Y1;

			return new IntPoint2(x, y);
		}

		public bool Contains(IntPoint3 ml)
		{
			return m_bounds.Contains(ml);
		}
	}
}