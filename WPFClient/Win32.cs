using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows;

namespace MyGame
{
	[Serializable]
	public class WindowPlacement
	{
		public IntPoint MinPosition { get; set; }
		public IntPoint MaxPosition { get; set; }
		public IntRect NormalPosition { get; set; }
		public bool ShowMaximized { get; set; }
	}
	
	static class Win32
	{
        #region Win32 API declarations to set and get window placement
        [DllImport("user32.dll")]
        static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

		// RECT structure required by WINDOWPLACEMENT structure
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public RECT(int left, int top, int right, int bottom)
			{
				this.Left = left;
				this.Top = top;
				this.Right = right;
				this.Bottom = bottom;
			}
		}

		// POINT structure required by WINDOWPLACEMENT structure
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		struct POINT
		{
			public int X;
			public int Y;

			public POINT(int x, int y)
			{
				this.X = x;
				this.Y = y;
			}
		}

		// WINDOWPLACEMENT stores the position, size, and state of a window
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		struct WINDOWPLACEMENT
		{
			public int length;
			public int flags;
			public int showCmd;
			public POINT minPosition;
			public POINT maxPosition;
			public RECT normalPosition;
		}
    
        const int SW_SHOWNORMAL = 1;
        const int SW_SHOWMINIMIZED = 2;
		const int SW_SHOWMAXIMIZED = 3;
        #endregion

		static public void LoadWindowPlacement(Window window, WindowPlacement placement)
		{
			try
			{
				// Load window placement details for previous application session from application settings
				// Note - if window was closed on a monitor that is now disconnected from the computer,
				//        SetWindowPlacement will place the window onto a visible monitor.
				WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
				wp.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
				wp.flags = 0;
				var p = placement;
				wp.showCmd = p.ShowMaximized ? SW_SHOWMAXIMIZED : SW_SHOWNORMAL;
				wp.minPosition = new POINT(p.MinPosition.X, p.MinPosition.Y);
				wp.maxPosition = new POINT(p.MaxPosition.X, p.MaxPosition.Y);
				wp.normalPosition = new RECT(p.NormalPosition.Left, p.NormalPosition.Top,
					p.NormalPosition.Right, p.NormalPosition.Bottom);
				IntPtr hwnd = new WindowInteropHelper(window).Handle;
				SetWindowPlacement(hwnd, ref wp);
			}
			catch { }
		}

		static public WindowPlacement SaveWindowPlacement(Window window)
		{
			WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
			IntPtr hwnd = new WindowInteropHelper(window).Handle;
			GetWindowPlacement(hwnd, out wp);
			var p = new WindowPlacement();
			p.MinPosition = new IntPoint(wp.minPosition.X, wp.minPosition.Y);
			p.MaxPosition = new IntPoint(wp.maxPosition.X, wp.maxPosition.Y);
			p.NormalPosition = new IntRect(wp.normalPosition.Left, wp.normalPosition.Top,
				wp.normalPosition.Right - wp.normalPosition.Left,
				wp.normalPosition.Bottom - wp.normalPosition.Top);
			p.ShowMaximized = wp.showCmd == SW_SHOWMAXIMIZED;
			return p;
		}
	}
}
