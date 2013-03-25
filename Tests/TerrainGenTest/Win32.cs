using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TerrainGenTest
{
	static class Win32
	{
		struct POINT
		{
			public int X;
			public int Y;
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll")]
		static extern bool SetCursorPos(int X, int Y);

		public static Point GetCursorPos()
		{
			POINT p;
			GetCursorPos(out p);
			return new Point(p.X, p.Y);
		}

		public static void SetCursorPos(Point p)
		{
			SetCursorPos((int)p.X, (int)p.Y);
		}
	}
}
