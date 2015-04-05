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
		MasterMapControl m_mapControl;
		Canvas m_canvas;
		Rectangle m_selectionRectangle;
		Rectangle m_cursorRectangle;

		MapSelection m_selection;

		MapSelectionMode m_selectionMode;

		public event Action<MapSelection> SelectionChanged;
		public event Action<MapSelection> GotSelection;
		public event Action<IntVector3> CursorPositionChanged;

		enum State
		{
			None,
			SelectingWithKeyboard,
			SelectingWithMouse,
		}

		State m_state;

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

			m_selectionRectangle = new Rectangle();
			m_selectionRectangle.Visibility = Visibility.Hidden;
			m_selectionRectangle.Stroke = Brushes.Blue;
			m_selectionRectangle.Stroke.Freeze();
			m_selectionRectangle.Fill = brush;
			m_selectionRectangle.Fill.Opacity = 0.2;
			m_selectionRectangle.Fill.Freeze();
			m_selectionRectangle.IsHitTestVisible = false;
			m_canvas.Children.Add(m_selectionRectangle);

			m_cursorRectangle = new Rectangle();
			m_cursorRectangle.Visibility = Visibility.Hidden;
			m_cursorRectangle.Stroke = Brushes.Yellow;
			m_cursorRectangle.Stroke.Freeze();
			m_cursorRectangle.IsHitTestVisible = false;
			m_canvas.Children.Add(m_cursorRectangle);
		}

		IntVector3 m_cursorPosition;

		public IntVector3 CursorPosition
		{
			get { return m_cursorPosition; }

			private set
			{
				if (value == m_cursorPosition)
					return;

				m_cursorPosition = value;

				if (this.CursorPositionChanged != null)
					this.CursorPositionChanged(value);
			}
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
						m_mapControl.ScreenCenterPosChanged -= OnScreenCenterPosChanged;
						m_mapControl.KeyDown -= OnKeyDown;

						break;

					case MapSelectionMode.Point:
						m_mapControl.MouseClicked -= OnMouseClicked;
						m_mapControl.KeyDown -= OnKeyDown;
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
						m_mapControl.ScreenCenterPosChanged += OnScreenCenterPosChanged;
						m_mapControl.KeyDown += OnKeyDown;

						break;

					case MapSelectionMode.Point:
						m_mapControl.MouseClicked += OnMouseClicked;
						m_mapControl.KeyDown += OnKeyDown;

						break;
				}

				this.CursorPosition = m_mapControl.MapCenterPos.ToIntVector3();

				UpdateCursorRectangle();
			}
		}

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (m_state == State.SelectingWithMouse)
				return;

			var key = e.Key;

			if (KeyHelpers.KeyIsDir(key))
			{
				m_mapControl.ScrollStop();

				var dir = m_mapControl.ScreenToMap(KeyHelpers.KeyToDir(key));

				if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					m_mapControl.ScreenCenterPos += dir;
				}
				else
				{
					int m = 1;

					if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
						m = 5;

					this.CursorPosition += dir.ToIntVector3() * m;

					m_mapControl.KeepOnScreen(this.CursorPosition);
					UpdateCursorRectangle();

					if (m_state == State.SelectingWithKeyboard)
						UpdateSelection(this.CursorPosition);
				}

				e.Handled = true;
			}
			else if (e.Key == Key.Enter)
			{
				if (this.SelectionMode == MapSelectionMode.Point)
				{
					StartSelection(this.CursorPosition, State.SelectingWithKeyboard);
					EndSelection(this.CursorPosition);
				}
				else
				{
					if (m_state == State.None)
					{
						StartSelection(this.CursorPosition, State.SelectingWithKeyboard);
					}
					else if (m_state == State.SelectingWithKeyboard)
					{
						EndSelection(this.CursorPosition);
					}
					else
					{
						throw new Exception();
					}
				}

				e.Handled = true;
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

				UpdateSelectionRectangle();

				if (this.SelectionChanged != null)
					this.SelectionChanged(m_selection);
			}
		}

		void StartSelection(IntVector3 p, State state)
		{
			m_state = state;
			UpdateCursorRectangle();
			this.Selection = new MapSelection(p, p);
		}

		void UpdateSelection(IntVector3 p)
		{
			IntVector3 start;

			var end = p.Truncate(new IntGrid3(this.m_mapControl.Environment.Size));

			switch (m_selectionMode)
			{
				case MapSelectionMode.Rectangle:
					start = new IntVector3(this.Selection.SelectionStart.ToIntVector2(), end.Z);
					break;

				case MapSelectionMode.Box:
					start = this.Selection.SelectionStart;
					break;

				case MapSelectionMode.Point:
					start = this.Selection.SelectionStart;
					break;

				default:
					throw new Exception();
			}

			this.Selection = new MapSelection(start, end);
		}

		void EndSelection(IntVector3 p)
		{
			m_state = State.None;
			UpdateSelection(p);
			UpdateCursorRectangle();
			if (this.GotSelection != null)
				this.GotSelection(this.Selection);
		}

		void AbortSelection()
		{
			m_state = State.None;
			this.Selection = new MapSelection();
			UpdateCursorRectangle();
		}

		void OnMouseClicked(object sender, MouseButtonEventArgs e)
		{
			var ml = m_mapControl.RenderPointToMapLocation(e.GetPosition(m_mapControl));
			StartSelection(ml, State.SelectingWithMouse);
			EndSelection(ml);
		}

		void OnDragStarted(Point pos)
		{
			if (m_state == State.SelectingWithKeyboard)
				return;

			var ml = m_mapControl.RenderPointToMapLocation(pos);
			StartSelection(ml, State.SelectingWithMouse);
		}

		void OnDragEnded(Point pos)
		{
			if (m_state != State.SelectingWithMouse)
				return;

			var ml = m_mapControl.RenderPointToMapLocation(pos);
			EndSelection(ml);
		}

		void OnDragging(Point pos)
		{
			if (m_state != State.SelectingWithMouse)
				return;

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

			m_mapControl.ScrollToDirection(v.ToDirection(), 0.2);

			var ml = m_mapControl.RenderPointToMapLocation(pos);

			UpdateSelection(ml);
		}

		void OnDragAborted()
		{
			if (m_state != State.SelectingWithMouse)
				return;

			AbortSelection();
		}

		void OnScreenCenterPosChanged(object control, DoubleVector3 centerPos, IntVector3 diff)
		{
			Point pos = Mouse.GetPosition(m_mapControl);

			UpdateSelectionRectangle();
			UpdateCursorRectangle();
		}

		void OnTileLayoutChanged(IntSize2 gridSize, double tileSize)
		{
			var pos = Mouse.GetPosition(m_mapControl);

			UpdateSelectionRectangle();
			UpdateCursorRectangle();
		}

		void UpdateSelectionRectangle()
		{
			if (!this.Selection.IsSelectionValid)
			{
				m_selectionRectangle.Visibility = Visibility.Hidden;
				return;
			}

			var selBox = this.Selection.SelectionBox;

			double z1 = m_mapControl.MapToScreen(selBox.Corner1).Z;
			double z2 = m_mapControl.MapToScreen(selBox.Corner2).Z;

			double z = m_mapControl.ScreenCenterPos.Z;

			if (z1 > z || z2 < z)
			{
				m_selectionRectangle.Visibility = Visibility.Hidden;
				return;
			}

			var r = m_mapControl.MapCubeToRenderPointRect(selBox);

			var thickness = Math.Max(2, m_mapControl.TileSize / 8);

			Canvas.SetLeft(m_selectionRectangle, r.Left - thickness);
			Canvas.SetTop(m_selectionRectangle, r.Top - thickness);
			m_selectionRectangle.Width = r.Width + thickness * 2;
			m_selectionRectangle.Height = r.Height + thickness * 2;

			m_selectionRectangle.StrokeThickness = thickness;

			m_selectionRectangle.Visibility = Visibility.Visible;
		}

		void UpdateCursorRectangle()
		{
			if (this.SelectionMode == MapSelectionMode.None || m_state == State.SelectingWithMouse)
			{
				m_cursorRectangle.Visibility = Visibility.Hidden;
				return;
			}

			var thickness = Math.Max(2, m_mapControl.TileSize / 8);

			var p = m_mapControl.MapLocationToScreenTile(this.CursorPosition);
			p -= new Vector(0.5, 0.5);
			p = m_mapControl.ScreenToRenderPoint(p);
			p -= new Vector(thickness, thickness);

			Canvas.SetLeft(m_cursorRectangle, p.X);
			Canvas.SetTop(m_cursorRectangle, p.Y);
			m_cursorRectangle.Width = m_mapControl.TileSize + thickness * 2;
			m_cursorRectangle.Height = m_mapControl.TileSize + thickness * 2;

			m_cursorRectangle.StrokeThickness = thickness;

			m_cursorRectangle.Visibility = Visibility.Visible;
		}
	}

	struct MapSelection
	{
		public MapSelection(IntVector3 start, IntVector3 end)
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
		public IntVector3 SelectionStart { get; set; }
		public IntVector3 SelectionEnd { get; set; }

		public IntVector3 SelectionPoint
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

				return new IntGrid2Z(this.SelectionStart.ToIntVector2(), this.SelectionEnd.ToIntVector2(), this.SelectionStart.Z);
			}
		}
	}
}
