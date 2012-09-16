using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	enum KeyHandlerMode
	{
		MapControl,
		LivingControl,
	}

	class KeyHandler
	{
		MasterMapControl m_mapControl;

		public KeyHandler(MasterMapControl mapControl)
		{
			m_mapControl = mapControl;

			mapControl.KeyDown += OnKeyDown;
			mapControl.KeyUp += OnKeyUp;
			mapControl.TextInput += OnTextInput;

			this.Mode = KeyHandlerMode.MapControl;
		}

		public KeyHandlerMode Mode { get; set; }

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (this.Mode == KeyHandlerMode.MapControl)
				OnKeyDownMap(sender, e);
			else
			OnKeyDownLiving(sender, e);
		}

		void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (this.Mode == KeyHandlerMode.MapControl)
				OnKeyUpMap(sender, e);
			else
				OnKeyUpLiving(sender, e);
		}

		void OnTextInput(object sender, TextCompositionEventArgs e)
		{
			if (this.Mode == KeyHandlerMode.MapControl)
				OnTextInputMap(sender, e);
			else
				OnTextInputLiving(sender, e);
		}


		void OnKeyDownLiving(object sender, KeyEventArgs e)
		{
			var ob = App.MainWindow.FocusedObject;

			if (ob == null)
				return;

			var dir = KeyToDir(e.Key);

			if (dir != Direction.None)
			{
				var action = new MoveAction(dir);
				ob.RequestAction(action);
			}
		}

		void OnKeyUpLiving(object sender, KeyEventArgs e)
		{
			var ob = App.MainWindow.FocusedObject;

			if (ob == null)
				return;
		}

		void OnTextInputLiving(object sender, TextCompositionEventArgs e)
		{
			var ob = App.MainWindow.FocusedObject;

			if (ob == null)
				return;

			string text = e.Text;

			e.Handled = true;

			if (text == ">")
			{
			}
			else if (text == "<")
			{
			}
			else
			{
				e.Handled = false;
			}
		}




		void OnKeyDownMap(object sender, KeyEventArgs e)
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

		void OnKeyUpMap(object sender, KeyEventArgs e)
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

		void OnTextInputMap(object sender, TextCompositionEventArgs e)
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

		static Direction KeyToDir(Key key)
		{
			switch (key)
			{
				case Key.Up: return Direction.North;
				case Key.Down: return Direction.South;
				case Key.Left: return Direction.West;
				case Key.Right: return Direction.East;
				case Key.Home: return Direction.NorthWest;
				case Key.End: return Direction.SouthWest;
				case Key.PageUp: return Direction.NorthEast;
				case Key.PageDown: return Direction.SouthEast;
				default: return Direction.None;
			}
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
