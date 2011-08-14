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
	class DragHelper
	{
		UIElement m_control;
		bool m_enabled;
		bool m_dragging;
		Point m_startPos;

		public event Action<Point> DragStarted;
		public event Action<Point> Dragging;
		public event Action<Point> DragEnded;

		public DragHelper(UIElement control)
		{
			m_control = control;
			m_enabled = false;
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
					if (m_control.IsMouseCaptured)
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

			Point pos = e.GetPosition(m_control);
			m_startPos = pos;

			m_control.MouseMove += OnMouseMove;
			m_control.CaptureMouse();
		}

		void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
				return;

			var dragging = m_dragging;

			if (m_control.IsMouseCaptured)
				m_control.ReleaseMouseCapture();

			if (dragging)
			{
				Point pos = e.GetPosition(m_control);

				if (this.DragEnded != null)
					this.DragEnded(pos);
			}
		}

		void OnLostMouseCapture(object sender, MouseEventArgs e)
		{
			m_startPos = new Point();
			m_dragging = false;
			m_control.MouseMove -= OnMouseMove;
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			Point pos = e.GetPosition(m_control);

			if (m_dragging == false)
			{
				if ((pos - m_startPos).Length < 4)
					return;

				m_dragging = true;

				if (this.DragStarted != null)
					this.DragStarted(m_startPos);
			}

			if (this.Dragging != null)
				this.Dragging(pos);
		}
	}
}
