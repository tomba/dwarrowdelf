using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	class MainWindowCommandHandler
	{
		MainWindow m_mainWindow;

		public MainWindowCommandHandler(MainWindow mainWindow)
		{
			m_mainWindow = mainWindow;

			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.AutoAdvanceTurnCommand, AutoAdvanceTurnHandler));
			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.OpenConsoleCommand, OpenConsoleHandler));
			m_mainWindow.CommandBindings.Add(new CommandBinding(ClientCommands.OpenBuildItemDialogCommand, OpenBuildItemHandler));
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

		void OpenBuildItemHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var mapControl = m_mainWindow.MapControl;

			var p = mapControl.Selection.SelectionCuboid.Corner1;
			var env = mapControl.Environment;

			var building = env.GetBuildingAt(p);

			if (building == null)
				return;

			var dialog = new BuildItemDialog();
			dialog.Owner = m_mainWindow;
			dialog.SetContext(building);
			var res = dialog.ShowDialog();

			if (res == true)
			{
				building.AddBuildOrder(dialog.BuildableItem);
			}
		}

	}
}
