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
		}

		public void AddCommandBindings(GameMode mode)
		{
			// add common bindings
			m_mainWindow.CommandBindings.AddRange(new CommandBinding[] {
				new CommandBinding(ClientCommands.AutoAdvanceTurnCommand, AutoAdvanceTurnHandler),
				new CommandBinding(ClientCommands.OpenConsoleCommand, OpenConsoleHandler),
				new CommandBinding(ClientCommands.OpenFocusDebugCommand, OpenFocusDebugHandler),
			});

			// add mode specific bindings
			switch (mode)
			{
				case GameMode.Fortress:
					foreach (var kvp in ClientTools.ToolDatas)
					{
						var toolMode = kvp.Value.Mode;
						m_mainWindow.CommandBindings.Add(new CommandBinding(kvp.Value.Command,
							(s, e) => m_mainWindow.ClientTools.ToolMode = toolMode));
					}
					break;

				case GameMode.Adventure:
					m_mainWindow.CommandBindings.AddRange(new CommandBinding[] {
						new CommandBinding(ClientCommands.DropItemCommand, DropItemHandler),
						new CommandBinding(ClientCommands.GetItemCommand, GetItemHandler),
						new CommandBinding(ClientCommands.RemoveItemCommand, RemoveItemHandler),
						new CommandBinding(ClientCommands.WearItemCommand, WearItemHandler),
						new CommandBinding(ClientCommands.InventoryCommand, InventoryHandler),
					});
					break;
			}
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

			var obs = living.Inventory.OfType<ItemObject>().Where(o => o.IsEquipped);

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
				if (ob.IsArmor || ob.IsWeapon)
					action = new UnequipItemAction(ob);
				else
					throw new Exception();
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
				.Where(o => ((o.IsArmor || o.IsWeapon) && o.IsEquipped == false));

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
				if (ob.IsArmor || ob.IsWeapon)
					action = new EquipItemAction(ob);
				else
					throw new Exception();
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
