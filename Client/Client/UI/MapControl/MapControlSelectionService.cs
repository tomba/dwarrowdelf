using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	enum MapSelectionMode
	{
		None,
		Point,
		Rectangle,
		Box,
	}

	sealed class MapControlSelectionService
	{
		MapSelection m_selection;
		Rectangle m_selectionRect;

		MapSelectionMode m_selectionMode;

		MasterMapControl m_mapControl;
		Canvas m_canvas;

		bool m_selecting;

		public event Action<MapSelection> SelectionChanged;
		public event Action<MapSelection> GotSelection;

		public MapControlSelectionService(MasterMapControl mapControl, Canvas canvas)
		{
			m_canvas = canvas;
			m_mapControl = mapControl;

			var brush = new LinearGradientBrush();
			brush.GradientStops.Add(new GradientStop(Colors.Blue, 0.0));
			brush.GradientStops.Add(new GradientStop(Colors.LightBlue, 0.5));
			brush.GradientStops.Add(new GradientStop(Colors.Blue, 1.0));
			brush.StartPoint = new Point(0.5, 0);
			brush.EndPoint = new Point(0.5, 1);

			m_selectionRect = new Rectangle();
			m_selectionRect.Visibility = Visibility.Hidden;
			m_selectionRect.Stroke = new SolidColorBrush(Colors.Blue);
			m_selectionRect.Stroke.Opacity = 0.6;
			m_selectionRect.Stroke.Freeze();
			m_selectionRect.StrokeThickness = 1;
			m_selectionRect.Fill = brush;
			m_selectionRect.Fill.Opacity = 0.2;
			m_selectionRect.Fill.Freeze();
			m_selectionRect.IsHitTestVisible = false;
			m_canvas.Children.Add(m_selectionRect);
		}

		public MapSelectionMode SelectionMode
		{
			get { return m_selectionMode; }
			set
			{
				if (value == m_selectionMode)
					return;

				switch (m_selectionMode)
				{
					case MapSelectionMode.Rectangle:
					case MapSelectionMode.Box:

						m_mapControl.DragStarted -= OnDragStarted;
						m_mapControl.DragEnded -= OnDragEnded;
						m_mapControl.Dragging -= OnDragging;
						m_mapControl.DragAborted -= OnDragAborted;

						m_mapControl.TileLayoutChanged -= OnTileLayoutChanged;
						m_mapControl.ZChanged -= OnZChanged;

						break;

					case MapSelectionMode.Point:
						m_mapControl.MouseClicked -= OnMouseClicked;
						break;
				}

				this.Selection = new MapSelection();
				m_selectionMode = value;

				switch (m_selectionMode)
				{
					case MapSelectionMode.Rectangle:
					case MapSelectionMode.Box:

						m_mapControl.DragStarted += OnDragStarted;
						m_mapControl.DragEnded += OnDragEnded;
						m_mapControl.Dragging += OnDragging;
						m_mapControl.DragAborted += OnDragAborted;

						m_mapControl.TileLayoutChanged += OnTileLayoutChanged;
						m_mapControl.ZChanged += OnZChanged;

						break;

					case MapSelectionMode.Point:
						m_mapControl.MouseClicked += OnMouseClicked;
						break;
				}
			}
		}

		void OnMouseClicked(object sender, MouseButtonEventArgs e)
		{
			var ml = m_mapControl.ScreenPointToMapLocation(e.GetPosition(m_mapControl));
			this.Selection = new MapSelection(ml, ml);
			if (this.GotSelection != null)
				this.GotSelection(this.Selection);
		}

		public MapSelection Selection
		{
			get
			{
				return m_selection;
			}

			set
			{
				if (m_selection.IsSelectionValid == value.IsSelectionValid &&
					m_selection.SelectionStart == value.SelectionStart &&
					m_selection.SelectionEnd == value.SelectionEnd)
					return;

				m_selection = value;

				UpdateSelectionRect();

				if (this.SelectionChanged != null)
					this.SelectionChanged(m_selection);
			}
		}

		void OnDragStarted(Point pos)
		{
			m_selecting = true;
			var ml = m_mapControl.ScreenPointToMapLocation(pos);
			this.Selection = new MapSelection(ml, ml);
		}

		void OnDragEnded(Point pos)
		{
			m_selecting = false;
			if (this.GotSelection != null)
				this.GotSelection(this.Selection);
		}

		void OnDragging(Point pos)
		{
			int limit = 4;
			int speed = 1;

			int dx = 0;
			int dy = 0;

			if (m_mapControl.ActualWidth - pos.X < limit)
				dx = speed;
			else if (pos.X < limit)
				dx = -speed;

			if (m_mapControl.ActualHeight - pos.Y < limit)
				dy = speed;
			else if (pos.Y < limit)
				dy = -speed;

			var v = new IntVector2(dx, dy);

			m_mapControl.ScrollToDirection(v);

			UpdateSelection(pos);
		}

		void OnDragAborted()
		{
			m_selecting = false;
			this.Selection = new MapSelection();
		}

		void OnZChanged(int z)
		{
			Point pos = Mouse.GetPosition(m_mapControl);

			if (m_selecting)
				UpdateSelection(pos);
			else
				UpdateSelectionRect();
		}

		void OnTileLayoutChanged(IntSize2 gridSize, double tileSize, Point centerPos)
		{
			var pos = Mouse.GetPosition(m_mapControl);

			if (m_selecting)
				UpdateSelection(pos);

			UpdateSelectionRect();
		}

		void UpdateSelection(Point mousePos)
		{
			IntPoint3 start;

			var end = m_mapControl.ScreenPointToMapLocation(mousePos);

			end = end.Truncate(new IntGrid3(this.m_mapControl.Environment.Size));

			switch (m_selectionMode)
			{
				case MapSelectionMode.Rectangle:
					start = new IntPoint3(this.Selection.SelectionStart.ToIntPoint(), end.Z);
					break;

				case MapSelectionMode.Box:
					start = this.Selection.SelectionStart;
					break;

				default:
					throw new Exception();
			}

			this.Selection = new MapSelection(start, end);
		}

		void UpdateSelectionRect()
		{
			if (!this.Selection.IsSelectionValid)
			{
				m_selectionRect.Visibility = Visibility.Hidden;
				return;
			}

			if (this.Selection.SelectionBox.Z1 > m_mapControl.Z || this.Selection.SelectionBox.Z2 < m_mapControl.Z)
			{
				m_selectionRect.Visibility = Visibility.Hidden;
				return;
			}

			var ir = new IntGrid2(this.Selection.SelectionStart.ToIntPoint(), this.Selection.SelectionEnd.ToIntPoint());

			var r = m_mapControl.MapRectToScreenPointRect(ir);

			Canvas.SetLeft(m_selectionRect, r.Left);
			Canvas.SetTop(m_selectionRect, r.Top);
			m_selectionRect.Width = r.Width;
			m_selectionRect.Height = r.Height;

			m_selectionRect.Visibility = Visibility.Visible;
		}
	}

	struct MapSelection
	{
		public MapSelection(IntPoint3 start, IntPoint3 end)
			: this()
		{
			this.SelectionStart = start;
			this.SelectionEnd = end;
			this.IsSelectionValid = true;
		}

		public MapSelection(IntGrid3 box)
			: this()
		{
			if (box.Columns == 0 || box.Rows == 0 || box.Depth == 0)
			{
				this.IsSelectionValid = false;
			}
			else
			{
				this.SelectionStart = box.Corner1;
				this.SelectionEnd = box.Corner2;
				this.IsSelectionValid = true;
			}
		}

		public bool IsSelectionValid { get; set; }
		public IntPoint3 SelectionStart { get; set; }
		public IntPoint3 SelectionEnd { get; set; }

		public IntPoint3 SelectionPoint
		{
			get
			{
				return this.SelectionStart;
			}
		}

		public IntGrid3 SelectionBox
		{
			get
			{
				if (!this.IsSelectionValid)
					return new IntGrid3();

				return new IntGrid3(this.SelectionStart, this.SelectionEnd);
			}
		}

		public IntGrid2Z SelectionIntRectZ
		{
			get
			{
				if (!this.IsSelectionValid)
					return new IntGrid2Z();

				if (this.SelectionStart.Z != this.SelectionEnd.Z)
					throw new Exception();

				return new IntGrid2Z(this.SelectionStart.ToIntPoint(), this.SelectionEnd.ToIntPoint(), this.SelectionStart.Z);
			}
		}
	}
}
