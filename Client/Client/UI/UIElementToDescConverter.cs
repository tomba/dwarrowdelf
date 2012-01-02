using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace Dwarrowdelf.Client.UI
{
	sealed class UIElementToDescConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return "None";

			var elem = value as FrameworkElement;

			if (elem == null || String.IsNullOrEmpty(elem.Name))
				return value.GetType().Name;
			else
				return String.Format("{0} ({1})", value.GetType().Name, elem.Name);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
