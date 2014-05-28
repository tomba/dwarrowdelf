using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class MainWindowMouseStatusBar : UserControl
	{
		public MainWindowMouseStatusBar()
		{
			InitializeComponent();
		}
	}

	class CoordinateValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Point)
			{
				var p = (Point)value;
				return String.Format("{0:F2}, {1:F2}", p.X, p.Y);
			}

			if (value is IntPoint2)
			{
				var p = (IntPoint2)value;
				return String.Format("{0}, {1}", p.X, p.Y);
			}

			if (value is IntPoint3)
			{
				var p = (IntPoint3)value;
				return String.Format("{0}, {1}, {2}", p.X, p.Y, p.Z);
			}

			return "Unknown";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
