using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	class KeyHandler
	{
		MasterMapControl m_mapControl;

		public KeyHandler(MasterMapControl mapControl)
		{
			m_mapControl = mapControl;

			mapControl.KeyDown += OnKeyDown;
			mapControl.KeyUp += OnKeyUp;
			mapControl.TextInput += OnTextInput;
		}

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;

			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				SetScrollDirection();
			}
			else if (e.Key == Key.Add)
			{
				m_mapControl.ZoomIn();
			}
			else if (e.Key == Key.Subtract)
			{
				m_mapControl.ZoomOut();
			}
			else
			{
				e.Handled = false;
			}
		}

		void OnKeyUp(object sender, KeyEventArgs e)
		{
			e.Handled = true;

			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				SetScrollDirection();
			}
			else
			{
				e.Handled = false;
			}
		}

		void OnTextInput(object sender, TextCompositionEventArgs e)
		{
			string text = e.Text;

			e.Handled = true;

			if (text == ">")
			{
				m_mapControl.Z--;
			}
			else if (text == "<")
			{
				m_mapControl.Z++;
			}
			else
			{
				e.Handled = false;
			}
		}

		static bool KeyIsDir(Key key)
		{
			switch (key)
			{
				case Key.Up: break;
				case Key.Down: break;
				case Key.Left: break;
				case Key.Right: break;
				case Key.Home: break;
				case Key.End: break;
				case Key.PageUp: break;
				case Key.PageDown: break;
				default:
					return false;
			}
			return true;
		}

		void SetScrollDirection()
		{
			var dir = Direction.None;

			if (Keyboard.IsKeyDown(Key.Home))
				dir |= Direction.NorthWest;
			else if (Keyboard.IsKeyDown(Key.PageUp))
				dir |= Direction.NorthEast;
			if (Keyboard.IsKeyDown(Key.PageDown))
				dir |= Direction.SouthEast;
			else if (Keyboard.IsKeyDown(Key.End))
				dir |= Direction.SouthWest;

			if (Keyboard.IsKeyDown(Key.Up))
				dir |= Direction.North;
			else if (Keyboard.IsKeyDown(Key.Down))
				dir |= Direction.South;

			if (Keyboard.IsKeyDown(Key.Left))
				dir |= Direction.West;
			else if (Keyboard.IsKeyDown(Key.Right))
				dir |= Direction.East;

			var fast = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

			var v = IntVector2.FromDirection(dir);

			if (fast)
				v *= 4;

			m_mapControl.ScrollToDirection(v);
		}
	}
}
