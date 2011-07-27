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
		public static RoutedUICommand OpenStockpileDialogCommand;
		public static RoutedUICommand OpenBuildItemDialogCommand;
		public static RoutedUICommand OpenConstructBuildingDialogCommand;
		public static RoutedUICommand OpenDesignateDialogCommand;
		public static RoutedUICommand OpenSetTerrainDialogCommand;
		public static RoutedUICommand OpenCreateItemDialogCommand;

		static ClientCommands()
		{
			AutoAdvanceTurnCommand = new RoutedUICommand("Auto-advance turn", "AutoAdvanceTurn", typeof(ClientCommands));

			OpenStockpileDialogCommand = new RoutedUICommand("Open Stockpile Dialog", "OpenStockpileDialog", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Alt) });

			OpenBuildItemDialogCommand = new RoutedUICommand("Open Build Item Dialog", "BuildItemDialog", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.A, ModifierKeys.Alt) });

			OpenConstructBuildingDialogCommand = new RoutedUICommand("Open Construct Building Dialog", "ConstructBuildingDialog", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.B, ModifierKeys.Alt) });

			OpenDesignateDialogCommand = new RoutedUICommand("Open Designate Dialog", "OpenDesignateDialog", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.D, ModifierKeys.Alt) });

			OpenSetTerrainDialogCommand = new RoutedUICommand("Open Set Terrain Dialog", "OpenSetTerrainDialog", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.T, ModifierKeys.Alt) });

			OpenCreateItemDialogCommand = new RoutedUICommand("Open Create Item Dialog", "OpenCreateItemDialog", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.C, ModifierKeys.Alt) });
		}
	}
}
