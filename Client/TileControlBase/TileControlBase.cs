using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Dwarrowdelf.Client.TileControl
{
	public abstract class TileControlBase : FrameworkElement
	{
		protected IntSize m_gridSize;
		protected Point m_renderOffset;

		protected bool m_tileLayoutInvalid;
		protected bool m_tileDataInvalid;
		protected bool m_tileRenderInvalid;

		protected MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControl");

		public event Action<IntSize, Point> TileLayoutChanged;
		public event Action AboutToRender;


		public IntSize GridSize
		{
			get { return m_gridSize; }
		}


		public double TileSize
		{
			get { return (double)GetValue(TileSizeProperty); }
			set { SetValue(TileSizeProperty, value); }
		}

		public static readonly DependencyProperty TileSizeProperty =
				DependencyProperty.Register("TileSize", typeof(double), typeof(TileControlBase), new UIPropertyMetadata(16.0, OnTileSizeChanged, OnCoerceTileSize));

		static void OnTileSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var tc = (TileControlBase)d;
			var ts = (double)e.NewValue;

			tc.trace.TraceInformation("TileSize = {0}", ts);

			tc.UpdateTileLayout(tc.RenderSize);
		}

		static object OnCoerceTileSize(DependencyObject d, Object baseValue)
		{
			var ts = (double)baseValue;

			ts = MyMath.Clamp(ts, 64, 2);

			return ts;
		}

		public Point CenterPos
		{
			get { return (Point)GetValue(CenterPosProperty); }
			set { SetValue(CenterPosProperty, value); }
		}

		public static readonly DependencyProperty CenterPosProperty =
			DependencyProperty.Register("CenterPos", typeof(Point), typeof(TileControlBase), new UIPropertyMetadata(new Point(), OnCenterPosChanged));


		static void OnCenterPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var tc = (TileControlBase)d;

			var p = (Point)e.NewValue;
			var po = (Point)e.OldValue;

			var x = Math.Round(p.X);
			var y = Math.Round(p.Y);
			tc.trace.TraceVerbose("CenterPos {0:F2}    {1},{2}", p, x, y);

			if (Math.Round(p.X) != Math.Round(po.X) || Math.Round(p.Y) != Math.Round(po.Y))
				tc.InvalidateTileData();

			tc.UpdateTileLayout(tc.RenderSize);
		}






		protected void UpdateTileLayout(Size renderSize)
		{
			var tileSize = this.TileSize;

			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			var columns = (int)Math.Ceiling(renderSize.Width / tileSize + 1) | 1;
			var rows = (int)Math.Ceiling(renderSize.Height / tileSize + 1) | 1;

			var gridSize = new IntSize(columns, rows);

			if (gridSize != m_gridSize)
				InvalidateTileData();

			m_gridSize = gridSize;

			var renderOffsetX = (int)(renderWidth - tileSize * m_gridSize.Width) / 2;
			var renderOffsetY = (int)(renderHeight - tileSize * m_gridSize.Height) / 2;

			var cx = -(this.CenterPos.X - Math.Round(this.CenterPos.X)) * this.TileSize;
			var cy = (this.CenterPos.Y - Math.Round(this.CenterPos.Y)) * this.TileSize;

			m_renderOffset = new Point(renderOffsetX + cx, renderOffsetY + cy);

			m_tileLayoutInvalid = true;

			trace.TraceInformation("UpdateTileLayout(rs {0}, gs {1}, ts {2}) -> Off {3:F2}, Grid {4}", renderSize, m_gridSize, tileSize,
				m_renderOffset, m_gridSize);

			InvalidateTileRender();
		}





		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			trace.TraceInformation("OnRender");

			var renderSize = this.RenderSize;

			if (m_tileLayoutInvalid)
			{
				if (TileLayoutChanged != null)
					TileLayoutChanged(m_gridSize, this.CenterPos);
			}

			if (m_tileDataInvalid)
			{
				if (this.AboutToRender != null)
					this.AboutToRender();
			}

			Render(drawingContext, renderSize);

			m_tileLayoutInvalid = false;
			m_tileDataInvalid = false;
			m_tileRenderInvalid = false;

			trace.TraceInformation("OnRender End");
		}

		protected abstract void Render(System.Windows.Media.DrawingContext drawingContext, Size renderSize);


		public void InvalidateTileRender()
		{
			if (m_tileRenderInvalid == false)
			{
				trace.TraceInformation("InvalidateRender");
				m_tileRenderInvalid = true;
				InvalidateVisual();
			}
		}

		public void InvalidateTileData()
		{
			if (m_tileDataInvalid == false)
			{
				m_tileDataInvalid = true;
				InvalidateTileRender();
			}
		}




		Vector ScreenMapDiff { get { return new Vector(Math.Round(this.CenterPos.X), Math.Round(this.CenterPos.Y)); } }

		public Point MapLocationToScreenLocation(Point ml)
		{
			var gridSize = this.GridSize;

			var p = ml - this.ScreenMapDiff;
			p = new Point(p.X, -p.Y);
			p += new Vector(gridSize.Width / 2, gridSize.Height / 2);

			return p;
		}

		public Point ScreenLocationToMapLocation(Point sl)
		{
			var gridSize = this.GridSize;

			var v = sl - new Vector(gridSize.Width / 2, gridSize.Height / 2);
			v = new Point(v.X, -v.Y);
			v += this.ScreenMapDiff;

			return v;
		}

		public Point ScreenPointToMapLocation(Point p)
		{
			var sl = ScreenPointToScreenLocation(p);
			return ScreenLocationToMapLocation(sl);
		}

		public Point MapLocationToScreenPoint(Point ml)
		{
			var sl = MapLocationToScreenLocation(ml);
			return ScreenLocationToScreenPoint(sl);
		}

		public Point ScreenPointToScreenLocation(Point p)
		{
			p -= new Vector(m_renderOffset.X, m_renderOffset.Y);
			return new Point(p.X / this.TileSize - 0.5, p.Y / this.TileSize - 0.5);
		}

		public Point ScreenLocationToScreenPoint(Point loc)
		{
			loc.Offset(0.5, 0.5);
			var p = new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
			p += new Vector(m_renderOffset.X, m_renderOffset.Y);
			return p;
		}
	}
}
