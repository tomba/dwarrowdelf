using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Dwarrowdelf.Client.UI
{
	public sealed class ObjectIDConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var oid = (ObjectID)value;

			if (oid == ObjectID.NullObjectID)
				return "NULL";
			else if (oid == ObjectID.AnyObjectID)
				return "ANY";
			else
			{
				char c;
				switch (oid.ObjectType)
				{
					case Dwarrowdelf.ObjectType.Environment: c = 'E'; break;
					case Dwarrowdelf.ObjectType.Item: c = 'I'; break;
					case Dwarrowdelf.ObjectType.Living: c = 'L'; break;
					default: c = '?'; break;
				}

				return String.Format("{0}{1}", c, oid.Value);
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
