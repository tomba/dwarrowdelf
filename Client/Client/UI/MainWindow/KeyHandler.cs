using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	enum KeyHandlerMode
	{
		None,
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

			GameData.Data.GameModeChanged += (mode) =>
			{
				switch (mode)
				{
					case GameMode.Undefined:
						this.Mode = KeyHandlerMode.None;
						break;

					case GameMode.Adventure:
						this.Mode = KeyHandlerMode.LivingControl;
						break;

					case GameMode.Fortress:
						this.Mode = KeyHandlerMode.MapControl;
						break;
				}
			};

			this.Mode = KeyHandlerMode.None;
		}

		public KeyHandlerMode Mode { get; set; }

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			bool handled;

			switch (this.Mode)
			{
				case KeyHandlerMode.MapControl:
					handled = HandleKeyDownMap(e);
					break;

				case KeyHandlerMode.LivingControl:
					handled = HandleKeyDownLiving(e);
					break;

				default:
					return;
			}

			e.Handled = handled;
		}

		void OnKeyUp(object sender, KeyEventArgs e)
		{
			bool handled;

			switch (this.Mode)
			{
				case KeyHandlerMode.MapControl:
					handled = HandleKeyUpMap(e);
					break;

				case KeyHandlerMode.LivingControl:
					handled = HandleKeyUpLiving(e);
					break;

				default:
					return;
			}

			e.Handled = handled;
		}

		void OnTextInput(object sender, TextCompositionEventArgs e)
		{
			bool handled;

			switch (this.Mode)
			{
				case KeyHandlerMode.MapControl:
					handled = HandleTextInputMap(e);
					break;

				case KeyHandlerMode.LivingControl:
					handled = HandleTextInputLiving(e);
					break;

				default:
					return;
			}

			e.Handled = handled;
		}


		bool HandleKeyDownLiving(KeyEventArgs e)
		{
			var ob = GameData.Data.FocusedObject;

			if (ob == null)
				return false;

			var dir = KeyToDir(e.Key);

			if (dir != Direction.None)
			{
				var target = ob.Environment.GetContents(ob.Location + dir).OfType<LivingObject>().FirstOrDefault();

				if (target == null)
				{
					var action = new MoveAction(dir);
					action.GUID = new ActionGUID(ob.World.PlayerID, 0);
					ob.RequestAction(action);
				}
				else
				{
					var action = new AttackAction(target);
					action.GUID = new ActionGUID(ob.World.PlayerID, 0);
					ob.RequestAction(action);
				}

				return true;
			}

			switch (e.Key)
			{
				case Key.OemPeriod:
					GameData.Data.SendProceedTurn();
					return true;

				case Key.Add:
					m_mapControl.ZoomIn();
					return true;

				case Key.Subtract:
					m_mapControl.ZoomOut();
					return true;

				case Key.PageDown:
					m_mapControl.ScreenCenterPos += Direction.Down;
					return true;

				case Key.PageUp:
					m_mapControl.ScreenCenterPos += Direction.Up;
					return true;

				default:
					return false;
			}
		}

		bool HandleKeyUpLiving(KeyEventArgs e)
		{
			var ob = GameData.Data.FocusedObject;

			if (ob == null)
				return false;

			return false;
		}

		bool HandleTextInputLiving(TextCompositionEventArgs e)
		{
			var ob = GameData.Data.FocusedObject;

			if (ob == null)
				return false;

			string text = e.Text;

			switch (text)
			{
				case ">":
					{
						var action = new MoveAction(Direction.Down);
						action.GUID = new ActionGUID(ob.World.PlayerID, 0);
						ob.RequestAction(action);
					}
					return true;

				case "<":
					{
						var action = new MoveAction(Direction.Up);
						action.GUID = new ActionGUID(ob.World.PlayerID, 0);
						ob.RequestAction(action);
					}
					return true;

				default:
					return false;
			}
		}




		bool HandleKeyDownMap(KeyEventArgs e)
		{
			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
			{
				SetScrollDirection();
				return true;
			}
			/*
			if (KeyIsZoom(e.Key))
			{
				SetZoom();
				return true;
			}
			*/
			switch (e.Key)
			{
				case Key.OemPeriod:
					GameData.Data.SendProceedTurn();
					return true;

				case Key.Add:
					m_mapControl.ZoomIn();
					return true;

				case Key.Subtract:
					m_mapControl.ZoomOut();
					return true;

				case Key.PageDown:
					m_mapControl.ScreenCenterPos += Direction.Down;
					return true;

				case Key.PageUp:
					m_mapControl.ScreenCenterPos += Direction.Up;
					return true;

				default:
					return false;
			}
		}

		bool HandleKeyUpMap(KeyEventArgs e)
		{
			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift | e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
			{
				SetScrollDirection();
				return true;
			}
			/*
			if (KeyIsZoom(e.Key))
			{
				SetZoom();
				return true;
			}
			*/

			return false;
		}

		bool HandleTextInputMap(TextCompositionEventArgs e)
		{
			string text = e.Text;

			switch (text)
			{
				case ">":
					m_mapControl.ScreenCenterPos += Direction.Down;
					return true;

				case "<":
					m_mapControl.ScreenCenterPos += Direction.Up;
					return true;

				default:
					return false;
			}
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
				case Key.NumPad9:
					return true;
				default:
					return false;
			}
		}

		static bool KeyIsZoom(Key key)
		{
			switch (key)
			{
				case Key.Add:
				case Key.Subtract:
				case Key.OemPlus:
				case Key.OemMinus:
					return true;
				default:
					return false;
			}
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

			double speed = 1;

			if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
				speed = 4;
			else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
				speed = 0.1;

			m_mapControl.ScrollToDirection(dir, speed);
		}

		void SetZoom()
		{
			double zoom = 0;

			if (Keyboard.IsKeyDown(Key.Add) || Keyboard.IsKeyDown(Key.OemPlus))
				zoom = 1;
			else if (Keyboard.IsKeyDown(Key.Subtract) || Keyboard.IsKeyDown(Key.OemMinus))
				zoom = -1;

			m_mapControl.Zoom(zoom);
		}
	}
}
