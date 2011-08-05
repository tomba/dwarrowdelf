using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dwarrowdelf.Client.TileControl
{
	public delegate void TileLayoutChangedDelegate(IntSize gridSize, double tileSize, Point centerPos);

	public abstract class TileControlBase : FrameworkElement, IDisposable
	{
		IRenderer m_renderer;

		IntSize m_gridSize;
		Point m_renderOffset;

		bool m_tileLayoutInvalid;
		bool m_tileDataInvalid;
		bool m_tileRenderInvalid;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControl");

		/// <summary>
		/// Called before render if grid size, tilesize or centerpos have changed
		/// </summary>
		public event TileLayoutChangedDelegate TileLayoutChanged;

		/// <summary>
		/// Called before render if tile data has changed
		/// </summary>
		public event Action AboutToRender;

		public event Action<Point> CenterPosChanged;

		protected void SetRenderer(IRenderer renderer)
		{
			m_renderer = renderer;
		}

		public IntSize GridSize
		{
			get { return m_gridSize; }
		}

		public void Dispose()
		{
			m_renderer.Dispose();
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

			tc.trace.TraceVerbose("TileSize = {0}", ts);

			tc.UpdateTileLayout(tc.RenderSize);
		}

		static object OnCoerceTileSize(DependencyObject d, Object baseValue)
		{
			var ts = (double)baseValue;
			return Math.Max(ts, 1);
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

			if (tc.CenterPosChanged != null)
				tc.CenterPosChanged(p);
		}






		void UpdateTileLayout(Size renderSize)
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

			var renderOffsetX = (renderWidth - tileSize * m_gridSize.Width) / 2;
			var renderOffsetY = (renderHeight - tileSize * m_gridSize.Height) / 2;

			var cx = -(this.CenterPos.X - Math.Round(this.CenterPos.X)) * this.TileSize;
			var cy = (this.CenterPos.Y - Math.Round(this.CenterPos.Y)) * this.TileSize;

			m_renderOffset = new Point(Math.Round(renderOffsetX + cx), Math.Round(renderOffsetY + cy));

			m_tileLayoutInvalid = true;

			trace.TraceVerbose("UpdateTileLayout(rs {0}, gs {1}, ts {2}) -> Off {3:F2}, Grid {4}", renderSize, m_gridSize, tileSize,
				m_renderOffset, m_gridSize);

			InvalidateTileRender();
		}




		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			trace.TraceVerbose("ArrangeOverride({0})", arrangeBounds);

			var renderSize = arrangeBounds;
			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			//if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
			UpdateTileLayout(renderSize);

			return base.ArrangeOverride(arrangeBounds);
		}


		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			trace.TraceVerbose("OnRender");

			var renderSize = this.RenderSize;

			if (m_tileLayoutInvalid)
			{
				if (TileLayoutChanged != null)
					TileLayoutChanged(m_gridSize, this.TileSize, this.CenterPos);
			}

			if (m_tileDataInvalid)
			{
				if (this.AboutToRender != null)
					this.AboutToRender();
			}

			var ctx = new TileControl.RenderContext()
			{
				TileSize = this.TileSize,
				RenderGridSize = this.GridSize,
				RenderOffset = m_renderOffset,
				TileDataInvalid = m_tileDataInvalid,
				TileRenderInvalid = m_tileRenderInvalid,
			};

			m_renderer.Render(drawingContext, renderSize, ctx);

			m_tileLayoutInvalid = false;
			m_tileDataInvalid = false;
			m_tileRenderInvalid = false;

			trace.TraceVerbose("OnRender End");
		}

		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_renderer.SymbolDrawingCache; }
			set
			{
				if (m_renderer.SymbolDrawingCache != null)
					m_renderer.SymbolDrawingCache.DrawingsChanged -= OnSymbolDrawingCacheChanged;

				m_renderer.SymbolDrawingCache = value;
				InvalidateTileRender();

				if (m_renderer.SymbolDrawingCache != null)
					m_renderer.SymbolDrawingCache.DrawingsChanged += OnSymbolDrawingCacheChanged;
			}
		}

		void OnSymbolDrawingCacheChanged()
		{
			InvalidateTileRender();
		}

		public void InvalidateTileRender()
		{
			if (m_tileRenderInvalid == false)
			{
				trace.TraceVerbose("InvalidateRender");
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

		public IntPoint ScreenPointToIntScreenLocation(Point p)
		{
			p -= new Vector(m_renderOffset.X, m_renderOffset.Y);
			return new IntPoint((int)Math.Round(p.X / this.TileSize - 0.5), (int)Math.Round(p.Y / this.TileSize - 0.5));
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
