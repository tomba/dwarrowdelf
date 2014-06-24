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
	sealed partial class InstallItemDialog : Window
	{
		EnvironmentObject m_env;
		IntVector3 m_location;

		public InstallItemDialog()
		{
			InitializeComponent();
		}

		public void SetContext(EnvironmentObject env, IntVector3 location)
		{
			m_env = env;
			m_location = location;

			var items = m_env.ItemTracker.GetItemsByDistance(m_location,
				i => i.ItemInfo.IsInstallable && i.IsReserved == false && i.IsInstalled == false);

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
	}
}
