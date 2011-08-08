using System;
using System.Collections.Generic;
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

namespace Dwarrowdelf.Client
{
	/// <summary>
	/// Interaction logic for MainWindowTools.xaml
	/// </summary>
	partial class MainWindowTools : UserControl
	{
		public MainWindowTools()
		{
			this.InitializeComponent();
		}

		public event Action<ClientToolMode> ToolModeChanged;

		public ClientToolMode ToolMode
		{
			get { return (ClientToolMode)GetValue(ToolModeProperty); }
			set { SetValue(ToolModeProperty, value); }
		}

		public static readonly DependencyProperty ToolModeProperty =
			DependencyProperty.Register("ToolMode", typeof(ClientToolMode), typeof(MainWindowTools), 
			new UIPropertyMetadata(ClientToolMode.Info, new PropertyChangedCallback(ToolModeChangedCallback)));

		static void ToolModeChangedCallback(DependencyObject ob, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (MainWindowTools)ob;
			var mode = (ClientToolMode)args.NewValue;

			switch (mode)
			{
				case ClientToolMode.Info:
					ctrl.infoButton.IsChecked = true;
					break;

				case ClientToolMode.DesignationRemove:
				case ClientToolMode.DesignationMine:
				case ClientToolMode.DesignationStairs:
				case ClientToolMode.DesignationFellTree:
					ctrl.DesignationToolMode = mode.ToString();
					ctrl.designationButton.IsChecked = true;
					break;

				case ClientToolMode.SetTerrain:
					ctrl.setTerrain.IsChecked = true;
					break;

				case ClientToolMode.CreateItem:
					ctrl.createItem.IsChecked = true;
					break;

				case ClientToolMode.CreateLiving:
					ctrl.createLiving.IsChecked = true;
					break;

				case ClientToolMode.CreateStockpile:
					ctrl.createStockpile.IsChecked = true;
					break;

				default:
					throw new Exception();
			}

			if (ctrl.ToolModeChanged != null)
				ctrl.ToolModeChanged(mode);
		}

		public string DesignationToolMode
		{
			get { return (string)GetValue(DesignationToolModeProperty); }
			set { SetValue(DesignationToolModeProperty, value); }
		}

		public static readonly DependencyProperty DesignationToolModeProperty =
			DependencyProperty.Register("DesignationToolMode", typeof(string), typeof(MainWindowTools), new UIPropertyMetadata("DesignationMine"));




		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			var tag = (string)item.Header;

			this.DesignationToolMode = tag;

			if (this.designationButton.IsChecked == true)
			{
				var mode = (ClientToolMode)Enum.Parse(typeof(ClientToolMode), (string)tag);
				this.ToolMode = mode;
			}
		}

		private void RadioButton_Checked(object sender, RoutedEventArgs e)
		{
			var item = (RadioButton)sender;

			var mode = (ClientToolMode)Enum.Parse(typeof(ClientToolMode), (string)item.Content);

			this.ToolMode = mode;
		}
	}

	class ClientToolModeToBrushConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Members

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (values.Length != 2)
				throw new Exception("illegal length");

			if (values[0] == null)
				throw new Exception("res dir null");

			if (!(values[0] is ResourceDictionary))
				return null;
			//throw new Exception(String.Format("illegal res type {0}", values[0].GetType().Name));

			var resDictionary = (ResourceDictionary)values[0];

			object value = values[1];

			if (value == null)
				return null;

			ClientToolMode mode;

			if (value is string)
				mode = (ClientToolMode)Enum.Parse(typeof(ClientToolMode), (string)value);
			else
				throw new Exception(String.Format("illegal type {0}", value.GetType().Name));

			string str = mode.ToString();
			var data = resDictionary[str];
			return data;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}


	public enum ClientToolMode
	{
		Info,
		DesignationRemove,
		DesignationMine,
		DesignationStairs,
		DesignationFellTree,
		SetTerrain,
		CreateStockpile,
		CreateItem,
		CreateLiving,
	}
}
