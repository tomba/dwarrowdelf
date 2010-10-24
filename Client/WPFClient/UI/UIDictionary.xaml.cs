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

			UserControl content;

			if (ob is ItemObject)
				content = new ItemInfoControl();
			else if (ob is Living)
				content = new LivingInfoControl();
			else
				throw new Exception();

			content.DataContext = ob;

			var dockableContent = new DockableContent()
			{
				Title = ob.ToString(),
				HideOnClose = false,
				IsCloseable = true,
				Content = content,
			};

			dockableContent.ShowAsFloatingWindow(GameData.Data.MainWindow.Dock, true);
		}
	}
}
