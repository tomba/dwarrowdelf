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
	internal partial class MapToolBar : UserControl
	{
		static MapToolBar()
		{
			ToolDatas = new Dictionary<ClientToolMode, ToolData>();

			Action<ClientToolMode, string, Key, string> add = (i, n, k, g) => ToolDatas[i] = new ToolData(i, n, k, g);

			add(ClientToolMode.Info, "Info", Key.Escape, "");

			add(ClientToolMode.DesignationMine, "Mine", Key.M, "Designate");
			add(ClientToolMode.DesignationStairs, "Mine stairs", Key.S, "Designate");
			add(ClientToolMode.DesignationChannel, "Channel", Key.C, "Designate");
			add(ClientToolMode.DesignationFellTree, "Fell tree", Key.F, "Designate");
			add(ClientToolMode.DesignationRemove, "Remove", Key.R, "Designate");

			add(ClientToolMode.CreateStockpile, "Create stockpile", Key.P, "");
			add(ClientToolMode.InstallFurniture, "Install furniture", Key.I, "");

			add(ClientToolMode.CreateLiving, "Create living", Key.L, "");
			add(ClientToolMode.CreateItem, "Create item", Key.Z, "");
			add(ClientToolMode.SetTerrain, "Set terrain", Key.T, "");
			add(ClientToolMode.ConstructBuilding, "Create building", Key.B, "");

			add(ClientToolMode.ConstructWall, "Wall", Key.W, "Construct");
			add(ClientToolMode.ConstructFloor, "Floor", Key.O, "Construct");
			add(ClientToolMode.ConstructPavement, "Pavement", Key.A, "Construct");
			add(ClientToolMode.ConstructRemove, "Remove", Key.E, "Construct");
		}

		public MapToolBar()
		{
			InitializeComponent();
		}

		private void ToolBar_Loaded(object sender, RoutedEventArgs e)
		{
			// Hide the overflow control
			ToolBar toolBar = sender as ToolBar;
			var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
			if (overflowGrid != null)
				overflowGrid.Visibility = Visibility.Collapsed;
		}

		public static readonly Dictionary<ClientToolMode, ToolData> ToolDatas;

		public event Action<ClientToolMode> ToolModeChanged;

		public ClientToolMode ToolMode
		{
			get { return (ClientToolMode)GetValue(ToolModeProperty); }
			set { SetValue(ToolModeProperty, value); }
		}

		public static readonly DependencyProperty ToolModeProperty =
			DependencyProperty.Register("ToolMode", typeof(ClientToolMode), typeof(MapToolBar),
			new UIPropertyMetadata(new PropertyChangedCallback(ToolModeChangedCallback)));

		static void ToolModeChangedCallback(DependencyObject ob, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (MapToolBar)ob;
			var mode = (ClientToolMode)args.NewValue;

			switch (mode)
			{
				case ClientToolMode.Info:
					ctrl.infoButton.IsChecked = true;
					break;

				case ClientToolMode.DesignationRemove:
				case ClientToolMode.DesignationMine:
				case ClientToolMode.DesignationStairs:
				case ClientToolMode.DesignationChannel:
				case ClientToolMode.DesignationFellTree:
					ctrl.DesignationToolMode = mode;
					ctrl.designationButton.IsChecked = true;
					break;

				case ClientToolMode.ConstructWall:
				case ClientToolMode.ConstructFloor:
				case ClientToolMode.ConstructPavement:
				case ClientToolMode.ConstructRemove:
					ctrl.ConstructToolMode = mode;
					ctrl.constructButton.IsChecked = true;
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

				case ClientToolMode.InstallFurniture:
					ctrl.installFurniture.IsChecked = true;
					break;

				case ClientToolMode.ConstructBuilding:
					ctrl.constructBuilding.IsChecked = true;
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
			DependencyProperty.Register("DesignationToolMode", typeof(ClientToolMode), typeof(MapToolBar), new UIPropertyMetadata(ClientToolMode.DesignationMine));


		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			var toolData = (ToolData)item.DataContext;

			this.DesignationToolMode = toolData.Mode;

			if (this.designationButton.IsChecked == true)
				this.ToolMode = toolData.Mode;
		}

		public ClientToolMode ConstructToolMode
		{
			get { return (ClientToolMode)GetValue(ConstructToolModeProperty); }
			set { SetValue(ConstructToolModeProperty, value); }
		}

		public static readonly DependencyProperty ConstructToolModeProperty =
			DependencyProperty.Register("ConstructToolMode", typeof(ClientToolMode), typeof(MapToolBar), new UIPropertyMetadata(ClientToolMode.ConstructWall));


		private void Construct_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			var toolData = (ToolData)item.DataContext;

			this.ConstructToolMode = toolData.Mode;

			if (this.constructButton.IsChecked == true)
				this.ToolMode = toolData.Mode;
		}

		private void RadioButton_Checked(object sender, RoutedEventArgs e)
		{
			var item = (RadioButton)sender;
			var toolData = (ToolData)item.DataContext;
			this.ToolMode = toolData.Mode;
		}
	}

	sealed class ToolData
	{
		public ToolData(ClientToolMode mode, string name, Key key, string groupName)
		{
			this.Mode = mode;
			this.Name = name;
			this.Key = key;
			this.ToolTip = String.Format("{0} ({1})", this.Name, key);
			this.GroupName = groupName;
		}

		public ClientToolMode Mode { get; private set; }
		public string Name { get; private set; }
		public string GroupName { get; private set; }
		public Key Key { get; private set; }
		public string ToolTip { get; private set; }
	}

	sealed class ClientToolModeToToolDataConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return null;

			var mode = (ClientToolMode)value;

			var data = MapToolBar.ToolDatas[mode];

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
		DesignationChannel,
		DesignationFellTree,
		SetTerrain,
		CreateStockpile,
		CreateItem,
		CreateLiving,
		ConstructBuilding,
		InstallFurniture,
		ConstructWall,
		ConstructFloor,
		ConstructPavement,
		ConstructRemove,
	}
}
