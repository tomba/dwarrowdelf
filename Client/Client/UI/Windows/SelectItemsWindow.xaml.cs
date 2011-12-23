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
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class SelectItemsWindow : Window
	{
		public SelectItemsWindow()
		{
			InitializeComponent();
		}

		public IEnumerable<ItemObject> SelectedItems { get; private set; }

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			this.SelectedItems = listBox.SelectedItems.OfType<ItemObject>().ToArray();
			this.DialogResult = true;
			this.Close();
		}
	}
}
