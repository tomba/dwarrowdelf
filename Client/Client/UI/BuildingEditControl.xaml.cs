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
using System.ComponentModel;

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
	sealed partial class BuildingEditControl : UserControl
	{
		public BuildingEditControl()
		{
			InitializeComponent();
		}

		private void DestructButtonClick(object sender, RoutedEventArgs e)
		{
		}

		private void CancelDestructButtonClick(object sender, RoutedEventArgs e)
		{
		}

		private void buildQueueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
				return;

			if (e.AddedItems.Count > 1)
				throw new Exception();

			var bo = (BuildOrder)e.AddedItems[0];

			buildOrderEditControl.EditableBuildOrder = new BuildOrderEditable(bo);
		}

		private void buildOrderEditControl_AddButtonClicked()
		{
			if (buildOrderEditControl.EditableBuildOrder.BuildableItem == null)
				return;

			var bo = buildOrderEditControl.EditableBuildOrder.ToBuildOrder();

			var building = (BuildItemManager)this.DataContext;
			building.AddBuildOrder(bo);
		}
	}
}
