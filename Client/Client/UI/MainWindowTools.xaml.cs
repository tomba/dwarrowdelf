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

namespace Dwarrowdelf.Client
{

	partial class MainWindowTools : UserControl
	{
		public static readonly Dictionary<ClientToolMode, ToolData> ToolDatas;

		static MainWindowTools()
		{
			var res = (ResourceDictionary)Application.LoadComponent(new Uri("/UI/ToolIcons.xaml", UriKind.Relative));

			ToolDatas = new Dictionary<ClientToolMode, ToolData>();

			Action<ClientToolMode, string, char> add = (i, n, k) => ToolDatas[i] = new ToolData(i, n, k, (DrawingBrush)res[i.ToString()]);

			add(ClientToolMode.Info, "Info", 'i');

			add(ClientToolMode.DesignationMine, "Mine", 'm');
			add(ClientToolMode.DesignationStairs, "Mine stairs", 's');
			add(ClientToolMode.DesignationFellTree, "Fell tree", 'f');
			add(ClientToolMode.DesignationRemove, "Remove", 'r');

			add(ClientToolMode.CreateStockpile, "Create stockpile", 'p');

			add(ClientToolMode.CreateLiving, "Create living", 'l');
			add(ClientToolMode.CreateItem, "Create item", 'i');
			add(ClientToolMode.SetTerrain, "Set terrain", 't');
		}

		public MainWindowTools()
		{
			this.InitializeComponent();

			this.ToolMode = ClientToolMode.Info;
		}

		public event Action<ClientToolMode> ToolModeChanged;

		public ClientToolMode ToolMode
		{
			get { return (ClientToolMode)GetValue(ToolModeProperty); }
			set { SetValue(ToolModeProperty, value); }
		}

		public static readonly DependencyProperty ToolModeProperty =
			DependencyProperty.Register("ToolMode", typeof(ClientToolMode), typeof(MainWindowTools),
			new UIPropertyMetadata(new PropertyChangedCallback(ToolModeChangedCallback)));

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
					ctrl.DesignationToolMode = mode;
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

		public ClientToolMode DesignationToolMode
		{
			get { return (ClientToolMode)GetValue(DesignationToolModeProperty); }
			set { SetValue(DesignationToolModeProperty, value); }
		}

		public static readonly DependencyProperty DesignationToolModeProperty =
			DependencyProperty.Register("DesignationToolMode", typeof(ClientToolMode), typeof(MainWindowTools), new UIPropertyMetadata(ClientToolMode.DesignationMine));


		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			var toolData = (ToolData)item.DataContext;

			this.DesignationToolMode = toolData.Mode;

			if (this.designationButton.IsChecked == true)
				this.ToolMode = toolData.Mode;
		}

		private void RadioButton_Checked(object sender, RoutedEventArgs e)
		{
			var item = (RadioButton)sender;
			var toolData = (ToolData)item.DataContext;
			this.ToolMode = toolData.Mode;
		}
	}

	class ToolData
	{
		public ToolData(ClientToolMode mode, string name, char key, DrawingBrush brush)
		{
			this.Mode = mode;
			this.Name = name;
			this.ToolTip = String.Format("{0} ({1})", this.Name, char.ToUpper(key));
			this.Brush = brush;
			this.Brush.Freeze();
		}

		public ClientToolMode Mode { get; private set; }
		public string Name { get; private set; }
		public string ToolTip { get; private set; }
		public DrawingBrush Brush { get; private set; }
	}

	class ClientToolModeToToolDataConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return null;

			var mode = (ClientToolMode)value;

			var data = MainWindowTools.ToolDatas[mode];

			return data;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	public enum ClientToolMode
	{
		None = 0,
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
