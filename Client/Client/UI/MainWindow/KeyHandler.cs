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

			var dpd = DependencyPropertyDescriptor.FromProperty(GameData.WorldProperty, typeof(GameData));
			dpd.AddValueChanged(GameData.Data, (s, e) =>
			{
				var world = GameData.Data.World;

				if (world == null || world.GameMode == GameMode.Undefined)
					this.Mode = KeyHandlerMode.None;
				else if (world.GameMode == GameMode.Adventure)
					this.Mode = KeyHandlerMode.LivingControl;
				else if (world.GameMode == GameMode.Fortress)
					this.Mode = KeyHandlerMode.MapControl;
				else
					throw new Exception();
			});

			this.Mode = KeyHandlerMode.LivingControl;
		}

		public KeyHandlerMode Mode { get; set; }

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			bool handled;

			if (this.Mode == KeyHandlerMode.MapControl)
				handled = HandleKeyDownMap(e);
			else if (this.Mode == KeyHandlerMode.LivingControl)
				handled = HandleKeyDownLiving(e);
			else
				throw new Exception();

			e.Handled = handled;
		}

		void OnKeyUp(object sender, KeyEventArgs e)
		{
			bool handled;

			if (this.Mode == KeyHandlerMode.MapControl)
				handled = HandleKeyUpMap(e);
			else if (this.Mode == KeyHandlerMode.LivingControl)
				handled = HandleKeyUpLiving(e);
			else
				throw new Exception();

			e.Handled = handled;
		}

		void OnTextInput(object sender, TextCompositionEventArgs e)
		{
			bool handled;

			if (this.Mode == KeyHandlerMode.MapControl)
				handled = HandleTextInputMap(e);
			else if (this.Mode == KeyHandlerMode.LivingControl)
				handled = HandleTextInputLiving(e);
			else
				throw new Exception();

			e.Handled = handled;
		}


		bool HandleKeyDownLiving(KeyEventArgs e)
		{
			var ob = App.MainWindow.FocusedObject;

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
					if (GameData.Data.User != null)
						GameData.Data.User.SendProceedTurn();
					return true;

				case Key.Add:
					m_mapControl.ZoomIn();
					return true;

				case Key.Subtract:
					m_mapControl.ZoomOut();
					return true;

				case Key.PageDown:
					m_mapControl.Z--;
					return true;

				case Key.PageUp:
					m_mapControl.Z++;
					return true;

				default:
					return false;
			}
		}

		bool HandleKeyUpLiving(KeyEventArgs e)
		{
			var ob = App.MainWindow.FocusedObject;

			if (ob == null)
				return false;

			return false;
		}

		bool HandleTextInputLiving(TextCompositionEventArgs e)
		{
			var ob = App.MainWindow.FocusedObject;

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
			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				SetScrollDirection();
				return true;
			}

			switch (e.Key)
			{
				case Key.OemPeriod:
					if (GameData.Data.User != null)
						GameData.Data.User.SendProceedTurn();
					return true;

				case Key.Add:
					m_mapControl.ZoomIn();
					return true;

				case Key.Subtract:
					m_mapControl.ZoomOut();
					return true;

				case Key.PageDown:
					m_mapControl.Z--;
					return true;

				case Key.PageUp:
					m_mapControl.Z++;
					return true;

				default:
					return false;
			}
		}

		bool HandleKeyUpMap(KeyEventArgs e)
		{
			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				SetScrollDirection();
				return true;
			}
			else
			{
				return false;
			}
		}

		bool HandleTextInputMap(TextCompositionEventArgs e)
		{
			string text = e.Text;

			switch (text)
			{
				case ">":
					m_mapControl.Z--;
					return true;

				case "<":
					m_mapControl.Z++;
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

			var v = new IntVector2(dir);

			if (fast)
				v *= 4;

			m_mapControl.ScrollToDirection(v);
		}
	}
}
