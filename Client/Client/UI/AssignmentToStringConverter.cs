using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Dwarrowdelf.Client.UI
{
	public sealed class AssignmentToStringConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Members

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var clientAssignment = (string)values[0];
			var serverAssignment = (string)values[1];

			// we may temporarily have both assignments set
			if (!String.IsNullOrEmpty(clientAssignment) && !String.IsNullOrEmpty(serverAssignment))
				return Binding.DoNothing;

			if (!String.IsNullOrEmpty(clientAssignment))
				return String.Format("{0} (Client)", clientAssignment);

			if (!String.IsNullOrEmpty(serverAssignment))
				return String.Format("{0} (Server)", serverAssignment);

			return "No assignment";
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
