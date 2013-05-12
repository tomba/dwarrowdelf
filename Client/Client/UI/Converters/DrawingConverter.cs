using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Data;

namespace Dwarrowdelf.Client.UI
{
	sealed class SymbolAndColorToDrawingConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Members

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			SymbolID symbolID;
			GameColor color;

			if (!(values[0] is SymbolID))
				symbolID = SymbolID.Undefined;
			else
				symbolID = (SymbolID)values[0];

			if (!(values[1] is GameColor))
				color = GameColor.None;
			else
				color = (GameColor)values[1];

			if (targetType != typeof(ImageSource))
				throw new ArgumentException();

			int tileSize;

			if (parameter == null)
				tileSize = 64;
			else
				tileSize = int.Parse((string)parameter);

			return GameData.Data.TileSet.GetTile(symbolID, color, tileSize);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	sealed class ItemIDAndMaterialIDToDrawingConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Members

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			SymbolID symbolID;
			GameColor color;

			if (!(values[0] is ItemID))
			{
				symbolID = SymbolID.Undefined;
				color = GameColor.None;
			}
			else
			{
				var itemID = (ItemID)values[0];

				symbolID = ItemSymbols.GetSymbol(itemID, false);

				if (values[1] is MaterialID)
				{
					var matID = (MaterialID)values[1];
					var matInfo = Materials.GetMaterial(matID);
					color = matInfo.Color;
				}
				else
				{
					color = GameColor.None;
				}
			}

			if (targetType != typeof(ImageSource))
				throw new ArgumentException();

			int tileSize;

			if (parameter == null)
				tileSize = 64;
			else
				tileSize = int.Parse((string)parameter);

			return GameData.Data.TileSet.GetTile(symbolID, color, tileSize);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
