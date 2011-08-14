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
using System.Diagnostics;

namespace Dwarrowdelf.Client.UI
{
	class DragHelper
	{
		enum State
		{
			None,
			Captured,
			Dragging,
		}

		UIElement m_control;
		bool m_enabled;
		Point m_startPos;
		State m_state;

		public event Action<Point> DragStarted;
		public event Action<Point> Dragging;
		public event Action<Point> DragEnded;
		public event Action DragAborted;

		public DragHelper(UIElement control)
		{
			m_control = control;
			m_enabled = false;
			m_state = State.None;
		}

		public bool IsEnabled
		{
			get { return m_enabled; }

			set
			{
				if (value == m_enabled)
					return;

				if (m_enabled)
				{
					if (m_state != State.None)
						m_control.ReleaseMouseCapture();

					m_control.MouseDown -= OnMouseDown;
					m_control.MouseUp -= OnMouseUp;
					m_control.LostMouseCapture -= OnLostMouseCapture;
				}

				m_enabled = value;

				if (m_enabled)
				{
					m_control.MouseDown += OnMouseDown;
					m_control.MouseUp += OnMouseUp;
					m_control.LostMouseCapture += OnLostMouseCapture;
				}
			}
		}

		void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
				return;

			m_state = State.Captured;

			Point pos = e.GetPosition(m_control);
			m_startPos = pos;

			m_control.MouseMove += OnMouseMove;
			m_control.CaptureMouse();
		}

		void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
				return;

			var state = m_state;
			m_state = State.None;

			if (state == State.Dragging)
			{
				Point pos = e.GetPosition(m_control);

				if (this.DragEnded != null)
					this.DragEnded(pos);
			}

			if (state != State.None)
				m_control.ReleaseMouseCapture();
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			Point pos = e.GetPosition(m_control);

			Debug.Assert(m_state != State.None);

			if (m_state == State.Captured)
			{
				if ((pos - m_startPos).Length < 2)
					return;

				m_state = State.Dragging;

				if (this.DragStarted != null)
					this.DragStarted(m_startPos);
			}

			if (this.Dragging != null)
				this.Dragging(pos);
		}

		void OnLostMouseCapture(object sender, MouseEventArgs e)
		{
			if (m_state == State.Dragging && this.DragAborted != null)
				this.DragAborted();

			m_startPos = new Point();
			m_state = State.None;
			m_control.MouseMove -= OnMouseMove;
		}
	}
}
