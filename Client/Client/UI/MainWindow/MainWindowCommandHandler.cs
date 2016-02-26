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
			});

			if (mode == GameMode.Undefined)
				return;

			// add common bindings
			bindings.AddRange(new[] {
				new CommandBinding(ClientCommands.AutoAdvanceTurnCommand, AutoAdvanceTurnHandler),
			});

			bindings.AddRange(new[] {
				new CommandBinding(ClientCommands.ToggleFullScreen, ToggleFullScreenHandler),
			});

			// add mode specific bindings
			switch (mode)
			{
				case GameMode.Fortress:
					foreach (var kvp in ClientTools.ToolDatas)
					{
						var toolMode = kvp.Value.Mode;
						bindings.Add(new CommandBinding(kvp.Value.Command,
							(s, e) => m_mainWindow.ClientTools.ToolMode = toolMode));
					}
					break;

				case GameMode.Adventure:
					break;
			}
		}

		void ToggleFullScreenHandler(object sender, ExecutedRoutedEventArgs e)
		{
			App.GameWindow.ToggleFullScreen();
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
			var dialog = new Dwarrowdelf.Client.UI.FocusDebugWindow();
			dialog.Owner = m_mainWindow;
			dialog.Show();
		}
	}
}
