using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace Dwarrowdelf.Client.TileControl
{
	public delegate void TileLayoutChangedDelegate(IntSize2 gridSize, double tileSize, Point centerPos);

	public abstract class TileControlCore : FrameworkElement
	{
		public IntSize2 GridSize { get; private set; }
		public Point RenderOffset { get; private set; }

		Size m_oldRenderSize;

		bool m_tileLayoutInvalid;
		bool m_tileDataInvalid;
		bool m_tileRenderInvalid;

		MyTraceSource trace = new MyTraceSource("Client.Render", "TileControl");

		/// <summary>
		/// Called before render if grid size, tilesize or centerpos have changed
		/// </summary>
		public event TileLayoutChangedDelegate TileLayoutChanged;

		public event Action<Point> CenterPosChanged;

		enum DragState
		{
			None,
			Captured,
			Dragging,
		}

		Point m_dragStartPos;
		DragState m_dragState;

		public event Action<Point> DragStarted;
		public event Action<Point> Dragging;
		public event Action<Point> DragEnded;
		public event Action DragAborted;

		protected TileControlCore()
		{
			m_dragState = DragState.None;

			this.MinHeight = 64;
			this.MinWidth = 64;
		}

		public double TileSize
		{
			get { return (double)GetValue(TileSizeProperty); }
			set { SetValue(TileSizeProperty, value); }
		}

		public static readonly DependencyProperty TileSizeProperty =
				DependencyProperty.Register("TileSize", typeof(double), typeof(TileControlCore), new UIPropertyMetadata(16.0, OnTileSizeChanged, OnCoerceTileSize));

		static void OnTileSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var tc = (TileControlCore)d;
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
			DependencyProperty.Register("CenterPos", typeof(Point), typeof(TileControlCore), new UIPropertyMetadata(new Point(), OnCenterPosChanged));


		static void OnCenterPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var tc = (TileControlCore)d;

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

			var gridSize = new IntSize2(columns, rows);

			if (gridSize != this.GridSize)
				InvalidateTileData();

			this.GridSize = gridSize;

			var renderOffsetX = (renderWidth - tileSize * this.GridSize.Width) / 2;
			var renderOffsetY = (renderHeight - tileSize * this.GridSize.Height) / 2;

			var cx = -(this.CenterPos.X - Math.Round(this.CenterPos.X)) * this.TileSize;
			var cy = -(this.CenterPos.Y - Math.Round(this.CenterPos.Y)) * this.TileSize;

			this.RenderOffset = new Point(Math.Round(renderOffsetX + cx), Math.Round(renderOffsetY + cy));

			m_tileLayoutInvalid = true;

			trace.TraceVerbose("UpdateTileLayout(rs {0}, gs {1}, ts {2}) -> Off {3:F2}, Grid {4}", renderSize, this.GridSize, tileSize,
				this.RenderOffset, this.GridSize);

			InvalidateTileRender();
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			trace.TraceVerbose("ArrangeOverride({0})", arrangeBounds);

			var renderSize = arrangeBounds;
			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			if (m_oldRenderSize != arrangeBounds)
				UpdateTileLayout(renderSize);

			m_oldRenderSize = arrangeBounds;

			var ret = base.ArrangeOverride(arrangeBounds);

			trace.TraceVerbose("ArrangeOverride end({0})", ret);

			return ret;
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			trace.TraceVerbose("OnRenderSizeChanged({0})", sizeInfo.NewSize);
			base.OnRenderSizeChanged(sizeInfo);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			trace.TraceVerbose("OnRender");

			var renderSize = this.RenderSize;

			if (m_tileLayoutInvalid)
			{
				if (TileLayoutChanged != null)
					TileLayoutChanged(this.GridSize, this.TileSize, this.CenterPos);
			}

			var ctx = new TileControl.TileRenderContext()
			{
				TileSize = this.TileSize,
				RenderGridSize = this.GridSize,
				RenderOffset = this.RenderOffset,
				TileDataInvalid = m_tileDataInvalid,
				TileRenderInvalid = m_tileRenderInvalid,
			};

			OnRenderTiles(drawingContext, renderSize, ctx);

			m_tileLayoutInvalid = false;
			m_tileDataInvalid = false;
			m_tileRenderInvalid = false;

			trace.TraceVerbose("OnRender End");
		}

		protected abstract void OnRenderTiles(DrawingContext drawingContext, Size renderSize, TileRenderContext ctx);

		/// <summary>
		/// Forces render, without resolving the tile data
		/// </summary>
		public void InvalidateTileRender()
		{
			if (m_tileRenderInvalid == false)
			{
				trace.TraceVerbose("InvalidateTileRender");
				m_tileRenderInvalid = true;
				InvalidateVisual();
			}
		}

		/// <summary>
		/// Forces render, resolving the tile data
		/// Note: Does NOT invalidate the tile data of RenderView
		/// </summary>
		public void InvalidateTileData()
		{
			if (m_tileDataInvalid == false)
			{
				trace.TraceVerbose("InvalidateTileData");
				m_tileDataInvalid = true;
				InvalidateTileRender();
			}
		}


		public static readonly RoutedEvent MouseClickedEvent =
			EventManager.RegisterRoutedEvent("MouseClicked", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(TileControlCore));

		public event MouseButtonEventHandler MouseClicked
		{
			add { AddHandler(MouseClickedEvent, value); }
			remove { RemoveHandler(MouseClickedEvent, value); }
		}


		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
			{
				base.OnMouseDown(e);
				return;
			}

			m_dragState = DragState.Captured;
			m_dragStartPos = e.GetPosition(this);
			CaptureMouse();

			e.Handled = true;
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
			{
				base.OnMouseUp(e);
				return;
			}

			var state = m_dragState;
			m_dragState = DragState.None;

			Point pos = e.GetPosition(this);

			switch (state)
			{
				case DragState.Captured:

					var newEvent = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
					{
						RoutedEvent = TileControlCore.MouseClickedEvent
					};
					RaiseEvent(newEvent);

					break;

				case DragState.Dragging:

					if (this.DragEnded != null)
						this.DragEnded(pos);

					break;
			}

			if (state != DragState.None)
				ReleaseMouseCapture();

			e.Handled = true;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (m_dragState == DragState.None)
			{
				base.OnMouseMove(e);
				return;
			}

			Point pos = e.GetPosition(this);

			if (m_dragState == DragState.Captured)
			{
				if ((pos - m_dragStartPos).Length < 2)
				{
					e.Handled = true;
					return;
				}

				m_dragState = DragState.Dragging;

				if (this.DragStarted != null)
					this.DragStarted(m_dragStartPos);
			}

			if (this.Dragging != null)
				this.Dragging(pos);

			e.Handled = true;
		}

		void OnLostMouseCapture(object sender, MouseEventArgs e)
		{
			if (m_dragState == DragState.Dragging && this.DragAborted != null)
				this.DragAborted();

			m_dragStartPos = new Point();
			m_dragState = DragState.None;
		}



		/**
		 * coordinate functions
		 */

		public Point ScreenPointToScreenTile(Point p)
		{
			p -= new Vector(this.RenderOffset.X, this.RenderOffset.Y);
			return new Point(p.X / this.TileSize - 0.5, p.Y / this.TileSize - 0.5);
		}

		public Point ScreenTileToScreenPoint(Point t)
		{
			var p = new Point((t.X + 0.5) * this.TileSize, (t.Y + 0.5) * this.TileSize);
			return p + new Vector(this.RenderOffset.X, this.RenderOffset.Y);
		}

		Vector ScreenMapDiff { get { return new Vector(Math.Round(this.CenterPos.X), Math.Round(this.CenterPos.Y)); } }

		public Point ScreenTileToMapTile(Point st)
		{
			var gridSize = this.GridSize;

			var v = st - new Vector(gridSize.Width / 2, gridSize.Height / 2);
			v += this.ScreenMapDiff;

			return v;
		}

		public Point MapTileToScreenTile(Point mt)
		{
			var gridSize = this.GridSize;

			var st = mt - this.ScreenMapDiff;
			st += new Vector(gridSize.Width / 2, gridSize.Height / 2);

			return st;
		}

		public Point ScreenPointToMapTile(Point p)
		{
			var st = ScreenPointToScreenTile(p);
			return ScreenTileToMapTile(st);
		}

		public Point MapTileToScreenPoint(Point mt)
		{
			var st = MapTileToScreenTile(mt);
			return ScreenTileToScreenPoint(st);
		}

		public IntPoint2 ScreenPointToIntScreenTile(Point p)
		{
			p -= new Vector(this.RenderOffset.X, this.RenderOffset.Y);
			return new IntPoint2((int)Math.Round(p.X / this.TileSize - 0.5), (int)Math.Round(p.Y / this.TileSize - 0.5));
		}
	}
}
