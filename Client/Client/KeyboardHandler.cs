using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Dwarrowdelf.Client
{
	enum KeyHandlerMode
	{
		None,
		FortressControl,
		LivingControl,
	}

	class KeyboardHandler
	{
		public KeyHandlerMode Mode { get; private set; }

		public KeyboardHandler()
		{
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
						this.Mode = KeyHandlerMode.FortressControl;
						break;
				}
			};

			this.Mode = KeyHandlerMode.None;
		}

		public void PreviewKeyDown(object sender, KeyEventArgs e)
		{
			bool handled;

			switch (this.Mode)
			{
				case KeyHandlerMode.FortressControl:
					handled = HandleKeyDownFortress(e);
					break;

				case KeyHandlerMode.LivingControl:
					handled = HandleKeyDownLiving(e);
					break;

				default:
					return;
			}

			e.Handled = handled;
		}

		public void PreviewKeyUp(object sender, KeyEventArgs e)
		{
			bool handled;

			switch (this.Mode)
			{
				case KeyHandlerMode.FortressControl:
					handled = HandleKeyUpFortress(e);
					break;

				case KeyHandlerMode.LivingControl:
					handled = HandleKeyUpLiving(e);
					break;

				default:
					return;
			}

			e.Handled = handled;
		}


		public void PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			bool handled;

			switch (this.Mode)
			{
				case KeyHandlerMode.FortressControl:
					handled = HandleTextInputFortress(e);
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

			var dir = KeyHelpers.KeyToDir(e.Key);

			if (dir != Direction.None)
			{
				CommandPromptHandler.HandleDirection(dir);
				return true;
			}

			switch (e.Key)
			{
				case Key.OemPeriod:
					GameData.Data.SendProceedTurn();
					return true;

				case Key.Enter:
					App.GameWindow.promptTextBox.Focus();
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
					CommandPromptHandler.HandleDirection(Direction.Down);
					return true;

				case "<":
					CommandPromptHandler.HandleDirection(Direction.Up);
					return true;

				default:
					return false;
			}
		}

		bool HandleKeyDownFortress(KeyEventArgs e)
		{
			return false;
		}

		bool HandleKeyUpFortress(KeyEventArgs e)
		{
			return false;
		}

		bool HandleTextInputFortress(TextCompositionEventArgs e)
		{
			return false;
		}
	}
}
