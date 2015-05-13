using System;
using System.Windows;
using System.Windows.Input;

namespace Dwarrowdelf.Client
{
	class SelectionService
	{
		MyGame m_game;
		SharpDXHost m_control;

		MapSelection m_selection;

		MapSelectionMode m_selectionMode;

		public event Action<MapSelection> SelectionChanged;
		public event Action<MapSelection> GotSelection;
		public event Action<IntVector3> KeyCursorPositionChanged;

		enum State
		{
			None,
			SelectingWithKeyboard,
			SelectingWithMouse,
		}

		State m_state;

		public SelectionService(MyGame game, SharpDXHost control, Camera camera)
		{
			m_game = game;
			m_control = control;
		}

		IntVector3 m_keyCursorPosition;

		public IntVector3 KeyCursorPosition
		{
			get { return m_keyCursorPosition; }

			private set
			{
				if (value == m_keyCursorPosition)
					return;

				m_keyCursorPosition = value;

				if (this.KeyCursorPositionChanged != null)
					this.KeyCursorPositionChanged(value);
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

						m_control.DragStarted -= OnDragStarted;
						m_control.DragEnded -= OnDragEnded;
						m_control.Dragging -= OnDragging;
						m_control.DragAborted -= OnDragAborted;

						m_control.KeyDown -= OnKeyDown;

						break;

					case MapSelectionMode.Point:
						m_control.MouseClicked -= OnMouseClicked;
						m_control.KeyDown -= OnKeyDown;
						break;
				}

				this.Selection = new MapSelection();
				m_selectionMode = value;

				switch (m_selectionMode)
				{
					case MapSelectionMode.Rectangle:
					case MapSelectionMode.Box:

						m_control.DragStarted += OnDragStarted;
						m_control.DragEnded += OnDragEnded;
						m_control.Dragging += OnDragging;
						m_control.DragAborted += OnDragAborted;

						m_control.KeyDown += OnKeyDown;

						break;

					case MapSelectionMode.Point:
						m_control.MouseClicked += OnMouseClicked;
						m_control.KeyDown += OnKeyDown;

						break;
				}

				if (m_selectionMode != MapSelectionMode.None)
				{
					// set key cursor to the center of the screen

					IntVector3 pos;
					Direction face;

					var view = m_game.Surfaces[0].Views[0];

					var center = new IntVector2((int)view.ViewPort.Width / 2, (int)view.ViewPort.Height / 2);

					bool hit = MousePositionService.PickVoxel(m_game, center, out pos, out face);

					if (hit)
						this.KeyCursorPosition = pos;
					else
						this.KeyCursorPosition = new IntVector3();
				}
			}
		}

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (m_state == State.SelectingWithMouse)
				return;

			var key = e.Key;

			if (KeyHelpers.KeyIsDir(key))
			{
				//m_control.ScrollStop();

				var dir = KeyHelpers.KeyToDir(key);

				if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					//m_control.ScreenCenterPos += dir;
				}
				else
				{
					int m = 1;

					if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
						m = 5;

					this.KeyCursorPosition += dir.ToIntVector3() * m;

					//m_control.KeepOnScreen(this.CursorPosition);
					//UpdateCursorRectangle();

					if (m_state == State.SelectingWithKeyboard)
						UpdateSelection(this.KeyCursorPosition);
				}

				e.Handled = true;
			}
			else if (e.Key == Key.Enter)
			{
				if (this.SelectionMode == MapSelectionMode.Point)
				{
					StartSelection(this.KeyCursorPosition, State.SelectingWithKeyboard);
					EndSelection(this.KeyCursorPosition);
				}
				else
				{
					if (m_state == State.None)
					{
						StartSelection(this.KeyCursorPosition, State.SelectingWithKeyboard);
					}
					else if (m_state == State.SelectingWithKeyboard)
					{
						EndSelection(this.KeyCursorPosition);
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

				if (this.SelectionChanged != null)
					this.SelectionChanged(m_selection);
			}
		}

		void StartSelection(IntVector3 p, State state)
		{
			m_state = state;
			this.Selection = new MapSelection(p, p);
		}

		void UpdateSelection(IntVector3 p)
		{
			IntVector3 start;

			var end = p.Truncate(new IntGrid3(m_game.Environment.Size));

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
			if (this.GotSelection != null)
				this.GotSelection(this.Selection);
		}

		void AbortSelection()
		{
			m_state = State.None;
			this.Selection = new MapSelection();
		}

		void OnMouseClicked(object sender, MouseButtonEventArgs e)
		{
			if (m_game.MousePositionService.MapLocation.HasValue == false)
				return;

			var ml = m_game.MousePositionService.MapLocation.Value;

			StartSelection(ml, State.SelectingWithMouse);
			EndSelection(ml);
		}

		void OnDragStarted(Point pos)
		{
			if (m_state == State.SelectingWithKeyboard)
				return;

			if (m_game.MousePositionService.MapLocation.HasValue == false)
				return;

			var ml = m_game.MousePositionService.MapLocation.Value;

			StartSelection(ml, State.SelectingWithMouse);
		}

		void OnDragEnded(Point pos)
		{
			if (m_state != State.SelectingWithMouse)
				return;

			if (m_game.MousePositionService.MapLocation.HasValue == false)
				return;

			var ml = m_game.MousePositionService.MapLocation.Value;

			EndSelection(ml);
		}

		void OnDragging(Point pos)
		{
			if (m_state != State.SelectingWithMouse)
				return;

#if scrolling
			int limit = 4;
			int speed = 1;

			int dx = 0;
			int dy = 0;

			if (m_control.ActualWidth - pos.X < limit)
				dx = speed;
			else if (pos.X < limit)
				dx = -speed;

			if (m_control.ActualHeight - pos.Y < limit)
				dy = speed;
			else if (pos.Y < limit)
				dy = -speed;

			var v = new IntVector2(dx, dy);

			m_control.ScrollToDirection(v.ToDirection(), 0.2);
#endif

			if (m_game.MousePositionService.MapLocation.HasValue == false)
				return;

			var ml = m_game.MousePositionService.MapLocation.Value;

			UpdateSelection(ml);
		}

		void OnDragAborted()
		{
			if (m_state != State.SelectingWithMouse)
				return;

			AbortSelection();
		}
	}
}
