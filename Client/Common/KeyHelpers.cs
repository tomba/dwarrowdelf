using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dwarrowdelf.Client
{
	public static class KeyHelpers
	{
		public static bool KeyIsShift(Key key)
		{
			return key == Key.LeftShift || key == Key.RightShift;
		}

		public static bool KeyIsControl(Key key)
		{
			return key == Key.LeftCtrl || key == Key.RightCtrl;
		}

		public static bool KeyIsDir(Key key)
		{
			switch (key)
			{
				case Key.Up:
				case Key.Down:
				case Key.Left:
				case Key.Right:
				case Key.NumPad1:
				case Key.NumPad2:
				case Key.NumPad3:
				case Key.NumPad4:
				case Key.NumPad5:
				case Key.NumPad6:
				case Key.NumPad7:
				case Key.NumPad8:
				case Key.NumPad9:
					return true;
				default:
					return false;
			}
		}

		public static Direction KeyToDir(Key key)
		{
			switch (key)
			{
				case Key.NumPad8:
				case Key.Up:
					return Direction.North;
				case Key.NumPad2:
				case Key.Down:
					return Direction.South;
				case Key.NumPad4:
				case Key.Left:
					return Direction.West;
				case Key.NumPad6:
				case Key.Right:
					return Direction.East;

				case Key.NumPad7: return Direction.NorthWest;
				case Key.NumPad1: return Direction.SouthWest;
				case Key.NumPad9: return Direction.NorthEast;
				case Key.NumPad3: return Direction.SouthEast;
				default: return Direction.None;
			}
		}
	}
}
