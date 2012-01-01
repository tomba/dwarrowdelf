using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class ListItemEditTemplateDictionary
	{
		public void Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;

			var ob = (BaseObject)button.DataContext;

			var dlg = new ObjectEditDialog();
			dlg.DataContext = ob;
			dlg.Owner = App.MainWindow;
			dlg.Show();
		}

		public void ControlButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var living = (LivingObject)button.DataContext;

			var wnd = new LivingControlWindow();
			wnd.DataContext = living;
			wnd.Owner = App.MainWindow;
			wnd.Show();
		}
	}
}
