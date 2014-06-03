using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace Dwarrowdelf.Client.TileControl
{
	public delegate void TileLayoutChangedNewDelegate(IntSize2 gridSize, double tileSize);

	public abstract class TileControlCoreNew : FrameworkElement
	{
		public IntSize2 GridSize { get; private set; }

		Vector m_renderOffset;

		Size m_oldRenderSize;

		bool m_tileDataInvalid;
		bool m_tileRenderInvalid;

		MyTraceSource trace = new MyTraceSource("Client.Render", "TileControl");

		/// <summary>
		/// Called before render if grid size, tilesize have changed
		/// </summary>
		public event TileLayoutChangedNewDelegate TileLayoutChanged;

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

		protected TileControlCoreNew()
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
				DependencyProperty.Register("TileSize", typeof(double), typeof(TileControlCoreNew),
				new UIPropertyMetadata(16.0, OnTileSizeChanged, OnCoerceTileSize));

		static void OnTileSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var tc = (TileControlCoreNew)d;
			var ts = (double)e.NewValue;

			tc.trace.TraceVerbose("TileSize = {0}", ts);

			tc.UpdateTileLayout(tc.RenderSize);
		}

		static object OnCoerceTileSize(DependencyObject d, Object baseValue)
		{
			var ts = (double)baseValue;
			return Math.Max(ts, 1);
		}

		void UpdateTileLayout(Size renderSize)
		{
			var tileSize = this.TileSize;

			UpdateGridSize(renderSize, tileSize);

			UpdateRenderOffset(renderSize, tileSize);

			trace.TraceVerbose("UpdateTileLayout(rs {0}, gs {1}, ts {2}) -> Off {3:F2}, Grid {4}", renderSize, this.GridSize, tileSize,
				m_renderOffset, this.GridSize);

			InvalidateTileRender();

			if (TileLayoutChanged != null)
				TileLayoutChanged(this.GridSize, tileSize);
		}

		void UpdateGridSize(Size renderSize, double tileSize)
		{
			var renderWidth = MyMath.Ceiling(renderSize.Width);
			var renderHeight = MyMath.Ceiling(renderSize.Height);

			var columns = MyMath.Ceiling(renderSize.Width / tileSize + 1) | 1;
			var rows = MyMath.Ceiling(renderSize.Height / tileSize + 1) | 1;

			var newGridSize = new IntSize2(columns, rows);

			if (this.GridSize != newGridSize)
			{
				this.GridSize = newGridSize;
				InvalidateTileData();
			}
		}

		void UpdateRenderOffset(Size renderSize, double tileSize)
		{
			var gridSize = this.GridSize;

			var renderOffsetX = (MyMath.Ceiling(renderSize.Width) - tileSize * gridSize.Width) / 2;
			var renderOffsetY = (MyMath.Ceiling(renderSize.Height) - tileSize * gridSize.Height) / 2;

			renderOffsetX -= m_tileOffset.X * tileSize;
			renderOffsetY -= m_tileOffset.Y * tileSize;

			m_renderOffset = new Vector(MyMath.Round(renderOffsetX), MyMath.Round(renderOffsetY));
		}

		Vector m_tileOffset;
		protected Vector TileOffset
		{
			get { return m_tileOffset; }

			set
			{
				if (m_tileOffset == value)
					return;

				m_tileOffset = value;
				trace.TraceVerbose("Offset = {0}", value);
				UpdateRenderOffset(this.RenderSize, this.TileSize);
				InvalidateTileRender();
			}
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			trace.TraceVerbose("ArrangeOverride({0})", arrangeBounds);

			if (m_oldRenderSize != arrangeBounds)
			{
				UpdateTileLayout(arrangeBounds);

				m_oldRenderSize = arrangeBounds;
			}

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

			var ctx = new TileControl.TileRenderContext()
			{
				TileSize = this.TileSize,
				RenderGridSize = this.GridSize,
				RenderOffset = m_renderOffset,
				TileDataInvalid = m_tileDataInvalid,
				TileRenderInvalid = m_tileRenderInvalid,
			};

			OnRenderTiles(drawingContext, this.RenderSize, ctx);

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
			EventManager.RegisterRoutedEvent("MouseClicked", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(TileControlCoreNew));

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
						RoutedEvent = TileControlCoreNew.MouseClickedEvent
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
		 * Render coordinate functions
		 */

		public Point RenderPointToRenderTile(Point p)
		{
			p -= m_renderOffset;
			return new Point(p.X / this.TileSize - 0.5, p.Y / this.TileSize - 0.5);
		}

		public Point RenderTileToRenderPoint(Point t)
		{
			var p = new Point((t.X + 0.5) * this.TileSize, (t.Y + 0.5) * this.TileSize);
			return p + m_renderOffset;
		}
	}
}
