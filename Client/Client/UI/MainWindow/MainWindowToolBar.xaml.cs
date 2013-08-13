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
using Dwarrowdelf.Messages;
using System.Threading.Tasks;
using System.Threading;

namespace Dwarrowdelf.Client.UI
{
	internal partial class MainWindowToolBar : UserControl
	{
		public MainWindowToolBar()
		{
			InitializeComponent();
		}

		private void Button_Click_Step(object sender, RoutedEventArgs e)
		{
			GameData.Data.SendProceedTurn();
		}



		void Button_LaborManager_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new LaborManagerDialog();
			dialog.Show();
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var cb = (ComboBox)sender;
			var cbi = (ComboBoxItem)cb.SelectedItem;
			int ms = int.Parse((string)cbi.Tag);

			if (GameData.Data.User != null)
			{
				GameData.Data.User.Send(new SetWorldConfigMessage()
				{
					MinTickTime = TimeSpan.FromMilliseconds(ms),
				});
			}
		}
	}

	class ComboBoxNullConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return "None";
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is string && ((string)value) == "None")
				return null;
			return value;
		}
	}
}
