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

			var ob = (BaseGameObject)button.DataContext;

			UserControl content;

			if (ob is ItemObject)
				content = new ItemInfoControl();
			else if (ob is Living)
				content = new LivingInfoControl();
			else if (ob is BuildingObject)
				content = new BuildingInfoControl();
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

			// XXX for some reason the DockableContent window seems to stay even after closed. This leads to the object being referenced.
			// This hack lets at least the object to be collected, although the window will stay in memory.
			dockableContent.Closed += (s2, e2) =>
				{
					var s = (DockableContent)s2;
					s.Content = null;
				};

			dockableContent.ShowAsFloatingWindow(GameData.Data.MainWindow.Dock, true);
		}
	}
}
