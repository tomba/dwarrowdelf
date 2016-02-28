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
		MapControl3D m_map;

		public MainWindowCommandHandler(MainWindow mainWindow)
		{
			m_mainWindow = mainWindow;
			m_map = mainWindow.MapControl;
		}

		public void AddCommandBindings(GameMode mode)
		{
			var bindings = m_map.CommandBindings;

			bindings.AddRange(new[] {
				new CommandBinding(ClientCommands.OpenConsoleCommand, OpenConsoleHandler),
				new CommandBinding(ClientCommands.OpenFocusDebugCommand, OpenFocusDebugHandler),
				new CommandBinding(ClientCommands.ToggleFullScreen, ToggleFullScreenHandler),
			});
		}

		void ToggleFullScreenHandler(object sender, ExecutedRoutedEventArgs e)
		{
			App.GameWindow.ToggleFullScreen();
		}

		void OpenConsoleHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new ConsoleDialog();
			dialog.Owner = m_mainWindow;
			dialog.Show();
		}

		void OpenFocusDebugHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new Dwarrowdelf.Client.UI.FocusDebugWindow();
			dialog.Owner = m_mainWindow;
			dialog.Show();
		}
	}
}
