using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AvalonDock;

namespace Dwarrowdelf.Client.UI
{
	partial class UIDictionary
	{
		public void Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;

			var ob = (ClientGameObject)button.DataContext;

			var dockableContent = new DockableContent()
			{
				Title = ob.ToString(),
				HideOnClose = false,
				IsCloseable = true,
				Content = new ObjectInfoControl()
				{
					DataContext = ob,
				},
			};

			dockableContent.ShowAsFloatingWindow(GameData.Data.MainWindow.Dock, true);
		}
	}
}
