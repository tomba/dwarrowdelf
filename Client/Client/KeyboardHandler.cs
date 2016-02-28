using Dwarrowdelf.Client.UI;
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

	abstract class GameKeyBinding
	{

	}

	sealed class GameRawKeyBinding : GameKeyBinding
	{
		public Key Key { get; }
		public ModifierKeys Mods { get; }
		public Action<KeyEventArgs> Action { get; }

		public GameRawKeyBinding(Key key, ModifierKeys modifiers, Action<KeyEventArgs> action)
		{
			this.Key = key;
			this.Mods = modifiers;
			this.Action = action;
		}
	}

	sealed class GameWildKeyBinding : GameKeyBinding
	{
		public Func<KeyEventArgs, bool> Action { get; }

		public GameWildKeyBinding(Func<KeyEventArgs, bool> action)
		{
			this.Action = action;
		}
	}

	sealed class GameTextKeyBinding : GameKeyBinding
	{
		public char Key { get; }
		public Action<TextCompositionEventArgs> Action { get; }

		public GameTextKeyBinding(char key, Action<TextCompositionEventArgs> action)
		{
			this.Key = key;
			this.Action = action;
		}
	}

	class KeyboardHandler
	{
		static GameKeyBinding CreateKey(Key key, Action<KeyEventArgs> action)
		{
			return CreateKey(key, ModifierKeys.None, action);
		}

		static GameKeyBinding CreateKey(Key key, ModifierKeys modifiers, Action<KeyEventArgs> action)
		{
			return new GameRawKeyBinding(key, modifiers, action);
		}

		static GameKeyBinding CreateKey(char key, Action<TextCompositionEventArgs> action)
		{
			return new GameTextKeyBinding(key, action);
		}

		static GameKeyBinding CreateKey(Func<KeyEventArgs, bool> action)
		{
			return new GameWildKeyBinding(action);
		}

		static GameKeyBinding[] s_commonKeyBindings = new[]
		{
			CreateKey(Key.Escape, e => App.GameWindow.ToolMode = ClientToolMode.Info),

			CreateKey(Key.Enter, e => App.GameWindow.promptTextBox.Focus()),
			CreateKey('.', e => GameData.Data.SendProceedTurn()),
			CreateKey(Key.Space, e => GameData.Data.IsAutoAdvanceTurn = !GameData.Data.IsAutoAdvanceTurn),

			CreateKey(Key.V, e => App.GameWindow.ToolMode = ClientToolMode.View),
			CreateKey(Key.I, ModifierKeys.Control, e => App.GameWindow.ToolMode = ClientToolMode.CreateItem),
			CreateKey(Key.L, ModifierKeys.Control, e => App.GameWindow.ToolMode = ClientToolMode.CreateLiving),
			CreateKey(Key.T, ModifierKeys.Control, e => App.GameWindow.ToolMode = ClientToolMode.SetTerrain),
		};

		static GameKeyBinding[] s_fortressKeyBindings = new[]
		{
			CreateKey(Key.M, e => App.GameWindow.ToolMode = ClientToolMode.DesignationMine),
			CreateKey(Key.S, e => App.GameWindow.ToolMode = ClientToolMode.DesignationStairs),
			CreateKey(Key.F, e => App.GameWindow.ToolMode = ClientToolMode.DesignationFellTree),
			CreateKey(Key.R, e => App.GameWindow.ToolMode = ClientToolMode.DesignationRemove),

			CreateKey(Key.W, e => App.GameWindow.ToolMode = ClientToolMode.ConstructWall),
			CreateKey(Key.O, e => App.GameWindow.ToolMode = ClientToolMode.ConstructFloor),
			CreateKey(Key.A, e => App.GameWindow.ToolMode = ClientToolMode.ConstructPavement),
			CreateKey(Key.E, e => App.GameWindow.ToolMode = ClientToolMode.ConstructRemove),

			CreateKey(Key.P, e => App.GameWindow.ToolMode = ClientToolMode.CreateStockpile),
			CreateKey(Key.I, e => App.GameWindow.ToolMode = ClientToolMode.InstallItem),
			CreateKey(Key.B, e => App.GameWindow.ToolMode = ClientToolMode.BuildItem),
		};

		static GameKeyBinding[] s_adventureKeyBindings = new[]
		{
			CreateKey('>', e => CommandPromptHandler.HandleDirection(Direction.Down)),
			CreateKey('<', e => CommandPromptHandler.HandleDirection(Direction.Up)),
			CreateKey(HandleAdventureCursorKeys),
		};

		public KeyHandlerMode Mode { get; private set; }

		public KeyboardHandler(MapControl3D mapControl)
		{
			mapControl.KeyDown += this.OnKeyDown;
			mapControl.KeyUp += this.OnKeyUp;
			mapControl.TextInput += this.OnTextInput;

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

		bool ProcessKeyEvent(KeyEventArgs e, GameKeyBinding[] bindings)
		{
			var key = e.Key;

			foreach (var b in bindings)
			{
				var binding = b as GameRawKeyBinding;

				if (binding != null)
				{
					if (binding.Key != key || binding.Mods != e.KeyboardDevice.Modifiers)
						continue;

					binding.Action(e);
					e.Handled = true;
					return true;
				}

				var wild = b as GameWildKeyBinding;

				if (wild != null)
				{
					if (wild.Action(e))
					{
						e.Handled = true;
						return true;
					}
				}
			}

			return false;
		}

		bool ProcessKeyEvent(TextCompositionEventArgs e, GameKeyBinding[] bindings)
		{
			if (e.Text.Length != 1)
				return false;

			char key = e.Text[0];

			foreach (var b in bindings)
			{
				var binding = b as GameTextKeyBinding;

				if (binding == null)
					continue;

				if (binding.Key != key)
					continue;

				binding.Action(e);
				e.Handled = true;
				return true;
			}

			return false;
		}

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (ProcessKeyEvent(e, s_commonKeyBindings))
				return;

			switch (this.Mode)
			{
				case KeyHandlerMode.FortressControl:
					if (ProcessKeyEvent(e, s_fortressKeyBindings))
						return;
					break;

				case KeyHandlerMode.LivingControl:
					if (ProcessKeyEvent(e, s_adventureKeyBindings))
						return;
					break;

				default:
					break;
			}
		}

		void OnKeyUp(object sender, KeyEventArgs e)
		{
		}

		void OnTextInput(object sender, TextCompositionEventArgs e)
		{
			if (ProcessKeyEvent(e, s_commonKeyBindings))
				return;

			switch (this.Mode)
			{
				case KeyHandlerMode.FortressControl:
					if (ProcessKeyEvent(e, s_fortressKeyBindings))
						return;
					break;

				case KeyHandlerMode.LivingControl:
					if (ProcessKeyEvent(e, s_adventureKeyBindings))
						return;
					break;

				default:
					break;
			}
		}

		static bool HandleAdventureCursorKeys(KeyEventArgs e)
		{
			var ob = GameData.Data.FocusedObject;

			if (ob == null)
				return false;

			var dir = KeyHelpers.KeyToDir(e.Key);

			if (dir == Direction.None)
				return false;

			CommandPromptHandler.HandleDirection(dir);

			return true;
		}
	}
}
