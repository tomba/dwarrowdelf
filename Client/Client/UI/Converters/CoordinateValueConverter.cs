using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Dwarrowdelf.Client.UI
{
	class CoordinateValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Point)
			{
				var p = (Point)value;
				return String.Format("{0:F2}, {1:F2}", p.X, p.Y);
			}

			if (value is DoubleVector3)
			{
				var p = (DoubleVector3)value;
				return String.Format("{0:F2}, {1:F2}, {2:F2}", p.X, p.Y, p.Z);
			}

			if (value is IntVector2)
			{
				var p = (IntVector2)value;
				return String.Format("{0}, {1}", p.X, p.Y);
			}

			if (value is IntVector3)
			{
				var p = (IntVector3)value;
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
