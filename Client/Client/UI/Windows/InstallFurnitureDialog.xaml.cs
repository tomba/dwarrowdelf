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
		EnvironmentObject m_env;
		IntPoint3 m_location;

		public InstallFurnitureDialog()
		{
			InitializeComponent();
		}

		public void SetContext(EnvironmentObject env, IntPoint3 location)
		{
			m_env = env;
			m_location = location;
			SetTargetItem(ItemID.Door);
		}

		void SetTargetItem(ItemID itemID)
		{
			if (m_env == null)
				return;

			var items = m_env.ItemTracker.GetItemsByDistance(m_location, ItemCategory.Furniture,
				i => i.ItemID == itemID && i.IsReserved == false && i.IsInstalled == false);

			listBox.ItemsSource = items;
		}

		private void Ok_Button_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		public ItemObject SelectedItem
		{
			get { return (ItemObject)listBox.SelectedItem; }
		}

		private void RadioButton_Checked(object sender, RoutedEventArgs e)
		{
			var b = (RadioButton)sender;

			var str = (string)b.Content;

			var id = (ItemID)Enum.Parse(typeof(ItemID), str);

			SetTargetItem(id);
		}
	}
}
