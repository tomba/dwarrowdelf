using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using SWF = System.Windows.Forms;

namespace Dwarrowdelf.Client
{
	public class SharpDXHost : HwndHost
	{
		MyRenderControl m_control;

		public SharpDXHost()
		{
			this.Focusable = true;

			this.MinWidth = 32;
			this.MinHeight = 32;

			m_control = new MyRenderControl();
		}

		public int HostedWindowWidth { get { return m_control.ClientSize.Width; } }
		public int HostedWindowHeight { get { return m_control.ClientSize.Height; } }

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

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			// Focus on mouse click
			Focus();
			base.OnMouseDown(e);
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
