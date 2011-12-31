using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AvalonDock;
using System.Windows.Media;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class TreeViewTemplateDictionary
	{
		void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var cm = (ContextMenu)menuItem.Parent;
			DependencyObject obj = cm.PlacementTarget;

			while (obj != null && !(obj is TreeViewItem))
				obj = VisualTreeHelper.GetParent(obj);

			var tvi = obj as TreeViewItem;
			var concreteObject = tvi.Header as ConcreteObject;

			if (concreteObject == null)
				return;

			var tag = (string)menuItem.Tag;

			switch (tag)
			{
				case "Goto":
					{
						var movable = concreteObject as MovableObject;

						if (movable == null || movable.Environment == null)
							return;

						App.MainWindow.MapControl.ScrollTo(movable.Environment, movable.Location);
					}
					break;

				case "Info":
					{
						var dlg = new ObjectEditDialog();
						dlg.DataContext = concreteObject;
						dlg.Owner = App.MainWindow;
						dlg.Show();

					}
					break;

				case "Control":
					{
						var living = concreteObject as LivingObject;

						var wnd = new LivingControlWindow();
						wnd.DataContext = living;
						wnd.Owner = App.MainWindow;
						wnd.Show();
					}
					break;

				default:
					throw new Exception();
			}
		}
	}
}
