using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Dwarrowdelf.Client
{
	public abstract class MapControlBase<T> : TileControl<T> where T : UIElement, new()
	{
		public IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, this.Rows / 2); }
		}

		public IntPoint BottomLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, -this.Rows / 2); }
		}

		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var sl = ScreenPointToScreenLocation(p);
			return ScreenLocationToMapLocation(sl);
		}

		public Point MapLocationToScreenPoint(IntPoint ml)
		{
			var sl = MapLocationToScreenLocation(ml);
			return ScreenLocationToScreenPoint(sl);
		}

		public IntPoint MapLocationToScreenLocation(IntPoint ml)
		{
			return new IntPoint(ml.X - this.TopLeftPos.X, -(ml.Y - this.TopLeftPos.Y));
		}

		public IntPoint ScreenLocationToMapLocation(IntPoint sl)
		{
			return new IntPoint(sl.X + this.TopLeftPos.X, -(sl.Y - this.TopLeftPos.Y));
		}

		IntPoint m_centerPos;
		public IntPoint CenterPos
		{
			get { return m_centerPos; }
			set
			{
				if (value == this.CenterPos)
					return;

				var v = value - this.CenterPos;
				ScrollTiles(v);

				m_centerPos = value;

				InvalidateTiles();
			}
		}
	}
}
