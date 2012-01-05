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
	sealed partial class InstallFurnitureDialog : Window
	{
		public InstallFurnitureDialog()
		{
			InitializeComponent();
		}

		private void Ok_Button_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		public ItemObject SelectedItem
		{
			get { return (ItemObject)listBox.SelectedItem; }
		}

		private void FilterItems(object sender, FilterEventArgs e)
		{
			var item = e.Item as ItemObject;

			if (item == null)
			{
				e.Accepted = false;
				return;
			}

			if (item.ItemCategory == ItemCategory.Furniture && item.IsReserved == false && item.IsInstalled == false)
				e.Accepted = true;
			else
				e.Accepted = false;
		}
	}
}
