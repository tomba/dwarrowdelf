using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	sealed class MainWindowCommandHandler
	{
		MainWindow m_mainWindow;

		public MainWindowCommandHandler(MainWindow mainWindow)
		{
			m_mainWindow = mainWindow;

			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.AutoAdvanceTurnCommand, AutoAdvanceTurnHandler));
			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.OpenConsoleCommand, OpenConsoleHandler));
			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.OpenFocusDebugCommand, OpenFocusDebugHandler));
		}

		public void AddAdventureCommands()
		{
			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.DropItemCommand, DropItemHandler));
			m_mainWindow.InputBindings.Add(new InputBinding(ClientCommands.DropItemCommand, new GameKeyGesture(Key.D)));

			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.GetItemCommand, GetItemHandler));
			m_mainWindow.InputBindings.Add(new InputBinding(ClientCommands.GetItemCommand, new GameKeyGesture(Key.OemComma)));

			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.RemoveItemCommand, RemoveItemHandler));
			m_mainWindow.InputBindings.Add(new InputBinding(ClientCommands.RemoveItemCommand, new GameKeyGesture(Key.R)));

			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.WearItemCommand, WearItemHandler));
			m_mainWindow.InputBindings.Add(new InputBinding(ClientCommands.WearItemCommand, new GameKeyGesture(Key.W)));

			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.InventoryCommand, InventoryHandler));
			m_mainWindow.InputBindings.Add(new InputBinding(ClientCommands.InventoryCommand, new GameKeyGesture(Key.I)));
		}

		void DropItemHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var living = m_mainWindow.FocusedObject;

			if (living == null)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = m_mainWindow;
			dlg.DataContext = living.Inventory;
			dlg.Title = "Drop Item";

			var ret = dlg.ShowDialog();

			if (ret.HasValue && ret.Value == true)
			{
				var ob = dlg.SelectedItem;

				var action = new DropItemAction(ob);
				action.MagicNumber = 1;
				living.RequestAction(action);
			}

			e.Handled = true;
		}

		void GetItemHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var living = m_mainWindow.FocusedObject;

			if (living == null)
				return;

			var obs = living.Environment.GetContents(living.Location).OfType<ItemObject>();

			if (obs.Any() == false)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = m_mainWindow;
			dlg.DataContext = obs;
			dlg.Title = "Get Item";

			var ret = dlg.ShowDialog();

			if (ret.HasValue && ret.Value == true)
			{
				var ob = dlg.SelectedItem;

				var action = new GetItemAction(ob);
				action.MagicNumber = 1;
				living.RequestAction(action);
			}

			e.Handled = true;
		}

		void RemoveItemHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var living = m_mainWindow.FocusedObject;

			if (living == null)
				return;

			var obs = living.Inventory.OfType<ItemObject>().Where(o => o.IsWorn || o.IsWielded);

			if (obs.Any() == false)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = m_mainWindow;
			dlg.DataContext = obs;
			dlg.Title = "Remove Item";

			var ret = dlg.ShowDialog();

			if (ret.HasValue && ret.Value == true)
			{
				var ob = dlg.SelectedItem;

				GameAction action;
				if (ob.IsArmor)
					action = new RemoveArmorAction(ob);
				else
					action = new RemoveWeaponAction(ob);
				action.MagicNumber = 1;
				living.RequestAction(action);
			}

			e.Handled = true;
		}

		void WearItemHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var living = m_mainWindow.FocusedObject;

			if (living == null)
				return;

			var obs = living.Inventory.OfType<ItemObject>()
				.Where(o => (o.IsArmor && o.IsWorn == false) || (o.IsWeapon && o.IsWielded == false));

			if (obs.Any() == false)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = m_mainWindow;
			dlg.DataContext = obs;
			dlg.Title = "Wear/Wield Item";

			var ret = dlg.ShowDialog();

			if (ret.HasValue && ret.Value == true)
			{
				var ob = dlg.SelectedItem;

				GameAction action;
				if (ob.IsArmor)
					action = new WearArmorAction(ob);
				else
					action = new WieldWeaponAction(ob);
				action.MagicNumber = 1;
				living.RequestAction(action);
			}

			e.Handled = true;
		}

		void InventoryHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var living = m_mainWindow.FocusedObject;

			if (living == null)
				return;

			var obs = living.Inventory;

			if (obs.Any() == false)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = m_mainWindow;
			dlg.DataContext = obs;
			dlg.Title = "Inventory";

			dlg.ShowDialog();
		}

		void AutoAdvanceTurnHandler(object sender, ExecutedRoutedEventArgs e)
		{
			GameData.Data.IsAutoAdvanceTurn = !GameData.Data.IsAutoAdvanceTurn;
		}

		void OpenConsoleHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new ConsoleDialog();
			dialog.Owner = m_mainWindow;
			dialog.Show();
		}

		void OpenFocusDebugHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new Dwarrowdelf.Client.UI.Windows.FocusDebugWindow();
			dialog.Owner = m_mainWindow;
			dialog.Show();
		}
	}
}
