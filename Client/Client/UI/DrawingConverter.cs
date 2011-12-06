using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Data;

namespace Dwarrowdelf.Client.UI
{
	class SymbolAndColorToDrawingConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Members

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(values[0] is SymbolID))
				return null;

			if (!(values[1] is GameColor))
				return null;

			if (targetType != typeof(Drawing))
				throw new ArgumentException();

			var symbolID = (SymbolID)values[0];
			var color = (GameColor)values[1];

			return GameData.Data.SymbolDrawingCache.GetDrawing(symbolID, color);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
