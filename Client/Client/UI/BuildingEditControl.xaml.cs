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
using System.Windows.Navigation;
using System.Windows.Shapes;

/// ITEM MANAGEMENT
/// add
/// cancel
/// promote/demote task
/// repeat
/// suspend
/// 
/// BUILDING MANAGEMENT
/// remove building
/// 
/// ITEMS (carpenter, wooden)
/// barrel
/// bucket
/// bed
/// chair
/// table
/// door
/// 
/// ITEMS (mason, stone)
/// block
/// door
/// chair
/// table
/// 

namespace Dwarrowdelf.Client.UI
{
	public partial class BuildingEditControl : UserControl
	{
		public BuildingEditControl()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var building = (BuildingObject)this.DataContext;

			var item = (BuildableItem)itemListBox.SelectedItem;

			building.AddBuildOrder(item);
		}
	}
}
