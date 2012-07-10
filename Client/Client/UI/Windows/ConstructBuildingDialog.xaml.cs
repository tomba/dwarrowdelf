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
	sealed partial class ConstructBuildingDialog : Window
	{
		public BuildingID BuildingID { get; private set; }

		public ConstructBuildingDialog()
		{
			InitializeComponent();

			var buildings = Enum.GetValues(typeof(BuildingID)).Cast<BuildingID>().Where(id => id != Dwarrowdelf.BuildingID.Undefined);

			buildingItemsControl.ItemsSource = buildings;
		}

		public void SetContext(EnvironmentObject env, IntGrid2Z area)
		{
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var type = (BuildingID)button.Content;

			this.BuildingID = type;

			this.DialogResult = true;
			Close();
		}
	}
}
