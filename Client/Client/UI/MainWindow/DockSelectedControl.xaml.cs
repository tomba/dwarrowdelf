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

namespace Dwarrowdelf.Client.UI
{
	sealed partial class DockSelectedControl : UserControl
	{
		public DockSelectedControl()
		{
			this.InitializeComponent();
		}
	}

	abstract class ListConverter<T> : IValueConverter
	{
		Func<T, string> m_converter;

		public ListConverter(Func<T, string> itemConverter)
		{
			m_converter = itemConverter;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return "";

			var list = (IEnumerable<T>)value;

			return String.Join(", ", list.Select(item => m_converter(item)));
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	sealed class MyInteriorsConverter : ListConverter<Tuple<InteriorInfo, MaterialInfo>>
	{
		public MyInteriorsConverter() : base(item => item.Item1.Name + " (" + item.Item2.Name + ")") { }
	}

	sealed class MyTerrainsConverter : ListConverter<Tuple<TerrainInfo, MaterialInfo>>
	{
		public MyTerrainsConverter() : base(item => item.Item1.Name + " (" + item.Item2.Name + ")") { }
	}

	sealed class MyWatersConverter : ListConverter<byte>
	{
		public MyWatersConverter() : base(item => item.ToString()) { }
	}

	sealed class MyBuildingsConverter : ListConverter<IDrawableElement>
	{
		public MyBuildingsConverter() : base(item => item.Description) { }
	}

	sealed class MyGrassesConverter : ListConverter<bool>
	{
		public MyGrassesConverter() : base(item => item.ToString()) { }
	}
}
