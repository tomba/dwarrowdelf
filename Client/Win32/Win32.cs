using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows;

namespace Win32
{
	[Serializable]
	public sealed class WindowPlacement
	{
		public int MinX { get; set; }
		public int MinY { get; set; }

		public int MaxX { get; set; }
		public int MaxY { get; set; }

		public int Left { get; set; }
		public int Top { get; set; }
		public int Right { get; set; }
		public int Bottom { get; set; }

		public bool ShowMaximized { get; set; }
	}

	public static class Helpers
	{
		static public void LoadWindowPlacement(Window window, WindowPlacement placement)
		{
			NativeMethods.LoadWindowPlacement(window, placement);
		}

		static public WindowPlacement SaveWindowPlacement(Window window)
		{
			return NativeMethods.SaveWindowPlacement(window);
		}
	}

	static class NativeMethods
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
				wp.minPosition = new POINT(p.MinX, p.MinY);
				wp.maxPosition = new POINT(p.MaxX, p.MaxY);
				wp.normalPosition = new RECT(p.Left, p.Top,
					p.Right, p.Bottom);
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
			p.MinX = wp.minPosition.X;
			p.MinY = wp.minPosition.Y;
			p.MaxX = wp.maxPosition.X;
			p.MaxY = wp.maxPosition.Y;
			p.Left = wp.normalPosition.Left;
			p.Top = wp.normalPosition.Top;
			p.Right = wp.normalPosition.Right;
			p.Bottom = wp.normalPosition.Bottom;
			p.ShowMaximized = wp.showCmd == SW_SHOWMAXIMIZED;
			return p;
		}
	}
}
