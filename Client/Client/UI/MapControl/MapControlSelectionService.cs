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
		Rectangle,
		Cuboid,
	}

	class MapControlSelectionService
	{
		MapSelection m_selection;
		Rectangle m_selectionRect;

		MapSelectionMode m_selectionMode;

		MasterMapControl m_mapControl;
		Canvas m_canvas;

		DragHelper m_dragHelper;

		public event Action<MapSelection> SelectionChanged;
		public event Action<MapSelection> GotSelection;

		public MapControlSelectionService(MasterMapControl mapControl, Canvas canvas)
		{
			m_canvas = canvas;
			m_mapControl = mapControl;

			m_selectionRect = new Rectangle();
			m_selectionRect.Visibility = Visibility.Hidden;
			m_selectionRect.Stroke = Brushes.Blue;
			m_selectionRect.StrokeThickness = 1;
			m_selectionRect.Fill = new SolidColorBrush(Colors.Blue);
			m_selectionRect.Fill.Opacity = 0.2;
			m_selectionRect.Fill.Freeze();
			m_canvas.Children.Add(m_selectionRect);

			m_dragHelper = new DragHelper(m_mapControl);
			m_dragHelper.DragStarted += OnDragStarted;
			m_dragHelper.DragEnded += OnDragEnded;
			m_dragHelper.Dragging += OnDragging;
		}

		public MapSelectionMode SelectionMode
		{
			get { return m_selectionMode; }
			set
			{
				if (value == m_selectionMode)
					return;

				if (m_selectionMode != MapSelectionMode.None)
				{
					m_mapControl.MapControl.TileLayoutChanged -= OnTileLayoutChanged;
					m_mapControl.MapControl.ZChanged -= OnZChanged;
				}

				this.Selection = new MapSelection();
				m_selectionMode = value;
				m_dragHelper.IsEnabled = m_selectionMode != MapSelectionMode.None;

				if (m_selectionMode != MapSelectionMode.None)
				{
					m_mapControl.MapControl.TileLayoutChanged += OnTileLayoutChanged;
					m_mapControl.MapControl.ZChanged += OnZChanged;
				}
			}
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
			var ml = m_mapControl.MapControl.ScreenPointToMapLocation(pos);
			this.Selection = new MapSelection(ml, ml);
		}

		void OnDragEnded(Point pos)
		{
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
				dy = -speed;
			else if (pos.Y < limit)
				dy = speed;

			var v = new IntVector(dx, dy);

			m_mapControl.ScrollToDirection(v);

			UpdateSelection(pos);
		}

		void OnZChanged(int z)
		{
			Point pos = Mouse.GetPosition(m_mapControl);

			if (m_mapControl.IsMouseCaptured)
				UpdateSelection(pos);
			else
				UpdateSelectionRect();
		}

		void OnTileLayoutChanged(IntSize gridSize, double tileSize, Point centerPos)
		{
			var pos = Mouse.GetPosition(m_mapControl);

			if (m_mapControl.IsMouseCaptured)
				UpdateSelection(pos);

			UpdateSelectionRect();
		}

		void UpdateSelection(Point mousePos)
		{
			IntPoint3D start;

			var end = m_mapControl.MapControl.ScreenPointToMapLocation(mousePos);

			switch (m_selectionMode)
			{
				case MapSelectionMode.Rectangle:
					start = new IntPoint3D(this.Selection.SelectionStart.ToIntPoint(), end.Z);
					break;

				case MapSelectionMode.Cuboid:
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

			if (this.Selection.SelectionCuboid.Z1 > m_mapControl.Z || this.Selection.SelectionCuboid.Z2 - 1 < m_mapControl.Z)
			{
				m_selectionRect.Visibility = Visibility.Hidden;
				return;
			}

			var ir = new IntRect(this.Selection.SelectionStart.ToIntPoint(), this.Selection.SelectionEnd.ToIntPoint());
			ir = ir.Inflate(1, 1);

			var r = m_mapControl.MapControl.MapRectToScreenPointRect(ir);

			Canvas.SetLeft(m_selectionRect, r.Left);
			Canvas.SetTop(m_selectionRect, r.Top);
			m_selectionRect.Width = r.Width;
			m_selectionRect.Height = r.Height;

			m_selectionRect.Visibility = Visibility.Visible;
		}
	}

	struct MapSelection
	{
		public MapSelection(IntPoint3D start, IntPoint3D end)
			: this()
		{
			this.SelectionStart = start;
			this.SelectionEnd = end;
			this.IsSelectionValid = true;
		}

		public MapSelection(IntCuboid cuboid)
			: this()
		{
			if (cuboid.Width == 0 || cuboid.Height == 0 || cuboid.Depth == 0)
			{
				this.IsSelectionValid = false;
			}
			else
			{
				this.SelectionStart = cuboid.Corner1;
				this.SelectionEnd = cuboid.Corner2 - new IntVector3D(1, 1, 1);
				this.IsSelectionValid = true;
			}
		}

		public bool IsSelectionValid { get; set; }
		public IntPoint3D SelectionStart { get; set; }
		public IntPoint3D SelectionEnd { get; set; }

		public IntCuboid SelectionCuboid
		{
			get
			{
				if (!this.IsSelectionValid)
					return new IntCuboid();

				return new IntCuboid(this.SelectionStart, this.SelectionEnd).Inflate(1, 1, 1);
			}
		}

		public IntRectZ SelectionIntRectZ
		{
			get
			{
				if (!this.IsSelectionValid)
					return new IntRectZ();

				if (this.SelectionStart.Z != this.SelectionEnd.Z)
					throw new Exception();

				return new IntRectZ(this.SelectionStart.ToIntPoint(), this.SelectionEnd.ToIntPoint(), this.SelectionStart.Z).Inflate(1, 1);
			}
		}
	}
}
