using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Dwarrowdelf.Client
{
	partial class UIDictionary
	{
		public void Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;

			var wnd = new ObjectInfoWindow();
			wnd.Owner = App.MainWindow;
			wnd.DataContext = button.DataContext;
			wnd.Show();
		}
	}
}
