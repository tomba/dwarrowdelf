using System;
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

namespace Dwarrowdelf.Client
{
	public static class ClientCommands
	{
		public static RoutedUICommand AutoAdvanceTurnCommand;
		public static RoutedUICommand OpenBuildItemDialogCommand;
		public static RoutedUICommand OpenConstructBuildingDialogCommand;
		public static RoutedUICommand OpenConsoleCommand;

		static ClientCommands()
		{
			AutoAdvanceTurnCommand = new RoutedUICommand("Auto-advance turn", "AutoAdvanceTurn", typeof(ClientCommands));

			OpenBuildItemDialogCommand = new RoutedUICommand("Open Build Item Dialog", "BuildItemDialog", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.A, ModifierKeys.Alt) });

			OpenConstructBuildingDialogCommand = new RoutedUICommand("Open Construct Building Dialog", "ConstructBuildingDialog", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.B, ModifierKeys.Alt) });

			OpenConsoleCommand = new RoutedUICommand("Open Console", "OpenConsole", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.Enter, ModifierKeys.Control) });
		}

	}
}
