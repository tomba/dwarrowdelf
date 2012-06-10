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
using Dwarrowdelf;

namespace ColorTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public GameColor[] ColorArray { get; private set; }

		public MainWindow()
		{
			ColorArray = EnumHelpers.GetEnumValues<GameColor>();

			InitializeComponent();

			var view = CollectionViewSource.GetDefaultView(grid.Items);
			view.Filter = Filter;
		}

		string m_filterStr = "";

		bool Filter(object ob)
		{
			if (string.IsNullOrWhiteSpace(m_filterStr))
				return true;

			var gc = (GameColor)ob;

			var str = gc.ToString().ToLowerInvariant();

			return str.ToLowerInvariant().Contains(m_filterStr);
		}

		private void textBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			m_filterStr = textBox.Text.ToLowerInvariant();

			var view = CollectionViewSource.GetDefaultView(grid.Items);
			view.Filter = Filter;
		}
	}

	public class GameColorToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var gc = (GameColor)value;
			var rgb = gc.ToGameColorRGB();
			return new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}
