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

			this.Mode = KeyHandlerMode.LivingControl;
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

			e.Handled = true;

			var dir = KeyToDir(e.Key);

			if (dir != Direction.None)
			{
				var action = new MoveAction(dir);
				action.MagicNumber = 1;
				ob.RequestAction(action);
			}
			else if (e.Key == Key.Add)
			{
				m_mapControl.ZoomIn();
			}
			else if (e.Key == Key.Subtract)
			{
				m_mapControl.ZoomOut();
			}
			else if (e.Key == Key.PageDown)
			{
				m_mapControl.Z--;
			}
			else if (e.Key == Key.PageUp)
			{
				m_mapControl.Z++;
			}
			else
			{
				e.Handled = false;
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
				var action = new MoveAction(Direction.Down);
				action.MagicNumber = 1;
				ob.RequestAction(action);
			}
			else if (text == "<")
			{
				var action = new MoveAction(Direction.Up);
				action.MagicNumber = 1;
				ob.RequestAction(action);
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
			else if (e.Key == Key.PageDown)
			{
				m_mapControl.Z--;
			}
			else if (e.Key == Key.PageUp)
			{
				m_mapControl.Z++;
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

			e.Handled = false;
		}

		static bool KeyIsDir(Key key)
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
					break;

				default:
					return false;
			}
			return true;
		}

		static Direction KeyToDir(Key key)
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

		void SetScrollDirection()
		{
			var dir = Direction.None;

			if (Keyboard.IsKeyDown(Key.NumPad7))
				dir |= Direction.NorthWest;
			else if (Keyboard.IsKeyDown(Key.NumPad9))
				dir |= Direction.NorthEast;

			if (Keyboard.IsKeyDown(Key.NumPad3))
				dir |= Direction.SouthEast;
			else if (Keyboard.IsKeyDown(Key.NumPad1))
				dir |= Direction.SouthWest;

			if (Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.NumPad8))
				dir |= Direction.North;
			else if (Keyboard.IsKeyDown(Key.Down) || Keyboard.IsKeyDown(Key.NumPad2))
				dir |= Direction.South;

			if (Keyboard.IsKeyDown(Key.Left) || Keyboard.IsKeyDown(Key.NumPad4))
				dir |= Direction.West;
			else if (Keyboard.IsKeyDown(Key.Right) || Keyboard.IsKeyDown(Key.NumPad6))
				dir |= Direction.East;

			var fast = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

			var v = IntVector2.FromDirection(dir);

			if (fast)
				v *= 4;

			m_mapControl.ScrollToDirection(v);
		}
	}
}
