using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using SWF = System.Windows.Forms;

namespace Dwarrowdelf.Client
{
	public class SharpDXHost : HwndHost
	{
		MyRenderControl m_control;

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

		public SharpDXHost()
		{
			this.Focusable = true;

			this.MinWidth = 32;
			this.MinHeight = 32;

			m_control = new MyRenderControl();
		}

		/// <summary>
		/// Width in Device Dependent Units
		/// </summary>
		public int HostedWindowWidth { get { return m_control.ClientSize.Width; } }

		/// <summary>
		/// Height in Device Dependent Units
		/// </summary>
		public int HostedWindowHeight { get { return m_control.ClientSize.Height; } }

		/// <summary>
		/// Mouse position in Device Dependent Units
		/// </summary>
		public Point MousePositionDeviceUnits
		{
			get
			{
				var p = SWF.Control.MousePosition;
				p = m_control.PointToClient(p);
				return new Point(p.X, p.Y);
			}
		}

		public static readonly RoutedEvent MouseClickedEvent =
			EventManager.RegisterRoutedEvent("MouseClicked", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(SharpDXHost));

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

			// Focus on mouse click
			Focus();

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
						RoutedEvent = SharpDXHost.MouseClickedEvent
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
					return;

				m_dragState = DragState.Dragging;

				if (this.DragStarted != null)
					this.DragStarted(m_dragStartPos);
			}

			if (this.Dragging != null)
				this.Dragging(pos);
		}

		void OnLostMouseCapture(object sender, MouseEventArgs e)
		{
			if (m_dragState == DragState.Dragging && this.DragAborted != null)
				this.DragAborted();

			m_dragStartPos = new Point();
			m_dragState = DragState.None;
		}




		protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
		{
			throw new NotImplementedException();
		}

		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			// Hit test always true, so that we get mouse move & down events
			return new PointHitTestResult(this, hitTestParameters.HitPoint);
		}

		protected override bool TabIntoCore(TraversalRequest request)
		{
			// Not sure if this is exactly right, but seems to work for allowing tab focusing into this
			Focus();
			return true;
		}

		protected override HandleRef BuildWindowCore(HandleRef hwndParent)
		{
			var childHandle = new HandleRef(this, m_control.Handle);

			SetParent(childHandle, hwndParent.Handle);

			return childHandle;
		}

		protected override void DestroyWindowCore(HandleRef hwnd)
		{
			SetParent(hwnd, IntPtr.Zero);
		}

		protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			const int WM_NCHITTEST = 0x0084;
			const int HTTRANSPARENT = (-1);

			if (msg == WM_NCHITTEST)
			{
				// pass all mouse events through the hwnd
				handled = true;
				return (IntPtr)HTTRANSPARENT;
			}

			return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
		}

		[DllImport("user32.dll", EntryPoint = "SetParent", CharSet = CharSet.Unicode)]
		static extern IntPtr SetParent(HandleRef hWnd, IntPtr hWndParent);

		class MyRenderControl : SWF.Control
		{
			public MyRenderControl()
			{
				SetStyle(SWF.ControlStyles.AllPaintingInWmPaint | SWF.ControlStyles.Opaque | SWF.ControlStyles.UserPaint, true);
				UpdateStyles();
			}

			protected override void OnPaintBackground(SWF.PaintEventArgs e)
			{
			}

			protected override void OnPaint(SWF.PaintEventArgs e)
			{
			}
		}
	}
}
