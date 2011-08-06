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

namespace Dwarrowdelf.Client
{
	partial class BuildItemDialog : Window
	{
		public BuildingObject Building;

		public BuildableItem BuildableItem { get { return (BuildableItem)itemListBox.SelectedItem; } }

		public BuildItemDialog()
		{
			InitializeComponent();
		}

		public void SetContext(BuildingObject building)
		{
			this.Building = building;

			buildingNameTextBlock.Text = this.Building.BuildingInfo.Name.Capitalize();

			mainGrid.DataContext = this.Building.BuildingInfo.BuildableItems;
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.BuildableItem == null)
				return;

			this.DialogResult = true;
		}
	}

	public class CapitalizeConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (targetType != typeof(string))
				return value;

			var str = (string)value;
			return str.Capitalize();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

}
