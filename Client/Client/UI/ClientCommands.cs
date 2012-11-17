﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	public static class ClientCommands
	{
		public static RoutedUICommand AutoAdvanceTurnCommand;
		public static RoutedUICommand OpenConsoleCommand;
		public static RoutedUICommand OpenFocusDebugCommand;
		public static RoutedUICommand DropItemCommand;
		public static RoutedUICommand GetItemCommand;
		public static RoutedUICommand RemoveItemCommand;
		public static RoutedUICommand WearItemCommand;
		public static RoutedUICommand InventoryCommand;

		static ClientCommands()
		{
			AutoAdvanceTurnCommand = new RoutedUICommand("Auto-advance turn", "AutoAdvanceTurn", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.Space) });

			OpenConsoleCommand = new RoutedUICommand("Open Console", "OpenConsole", typeof(ClientCommands),
				new InputGestureCollection() { new GameKeyGesture(Key.Enter, ModifierKeys.Control) });

			OpenFocusDebugCommand = new RoutedUICommand("Open FocusDebug", "OpenFocusDebug", typeof(ClientCommands));

			DropItemCommand = new RoutedUICommand("Drop Item", "DropItem", typeof(ClientCommands),
				new InputGestureCollection() { new GameKeyGesture(Key.D) });
			GetItemCommand = new RoutedUICommand("Get Item", "GetItem", typeof(ClientCommands),
				new InputGestureCollection() { new GameKeyGesture(Key.OemComma, ",") });

			RemoveItemCommand = new RoutedUICommand("Remove Item", "RemoveItem", typeof(ClientCommands),
				new InputGestureCollection() { new GameKeyGesture(Key.R) });
			WearItemCommand = new RoutedUICommand("Wear Item", "WearItem", typeof(ClientCommands),
				new InputGestureCollection() { new GameKeyGesture(Key.W) });

			InventoryCommand = new RoutedUICommand("Inventory", "Inventory", typeof(ClientCommands),
				new InputGestureCollection() { new GameKeyGesture(Key.I) });
		}
	}

	/* KeyGesture class doesn't like gestures without modifiers, so we need our own */
	public sealed class GameKeyGesture : KeyGesture
	{
		bool m_hackMode;

		public GameKeyGesture(Key key)
			: base(key, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt, key.ToString())
		{
			m_hackMode = true;
		}

		public GameKeyGesture(Key key, string displayString)
			: base(key, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt, displayString)
		{
			m_hackMode = true;
		}

		public GameKeyGesture(Key key, ModifierKeys modifiers)
			: base(key, modifiers)
		{
		}

		public override bool Matches(object targetElement, InputEventArgs inputEventArgs)
		{
			if (m_hackMode)
			{
				KeyEventArgs args = inputEventArgs as KeyEventArgs;
				return args != null && Keyboard.Modifiers == ModifierKeys.None && this.Key == args.Key;
			}
			else
			{
				return base.Matches(targetElement, inputEventArgs);
			}
		}
	}
}
