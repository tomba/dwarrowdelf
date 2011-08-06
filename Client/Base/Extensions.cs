using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	public static class MyExtensions
	{
		public static System.Windows.Media.Color ToWindowsColor(this GameColor color)
		{
			var rgb = color.ToGameColorRGB();
			return System.Windows.Media.Color.FromRgb(rgb.R, rgb.G, rgb.B);
		}
	}

}
