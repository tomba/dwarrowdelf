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
		ClientTools m_clientTools;

		public MapToolBar()
		{
			InitializeComponent();
		}

		public void SetClientTools(ClientTools clientTools)
		{
			m_clientTools = clientTools;
			OnToolModeChanged(clientTools.ToolMode);
			m_clientTools.ToolModeChanged += OnToolModeChanged;
		}

		private void ToolBar_Loaded(object sender, RoutedEventArgs e)
		{
			// Hide the overflow control
			ToolBar toolBar = sender as ToolBar;
			var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
			if (overflowGrid != null)
				overflowGrid.Visibility = Visibility.Collapsed;
		}

		void OnToolModeChanged(ClientToolMode mode)
		{
			switch (mode)
			{
				case ClientToolMode.Info:
					this.infoButton.IsChecked = true;
					break;

				case ClientToolMode.DesignationRemove:
				case ClientToolMode.DesignationMine:
				case ClientToolMode.DesignationStairs:
				case ClientToolMode.DesignationChannel:
				case ClientToolMode.DesignationFellTree:
					this.DesignationToolMode = mode;
					this.designationButton.IsChecked = true;
					break;

				case ClientToolMode.ConstructWall:
				case ClientToolMode.ConstructFloor:
				case ClientToolMode.ConstructPavement:
				case ClientToolMode.ConstructRemove:
					this.ConstructToolMode = mode;
					this.constructButton.IsChecked = true;
					break;

				case ClientToolMode.SetTerrain:
					this.setTerrain.IsChecked = true;
					break;

				case ClientToolMode.CreateItem:
					this.createItem.IsChecked = true;
					break;

				case ClientToolMode.CreateLiving:
					this.createLiving.IsChecked = true;
					break;

				case ClientToolMode.CreateStockpile:
					this.createStockpile.IsChecked = true;
					break;

				case ClientToolMode.InstallFurniture:
					this.installFurniture.IsChecked = true;
					break;

				case ClientToolMode.ConstructBuilding:
					this.constructBuilding.IsChecked = true;
					break;

				default:
					throw new Exception();
			}
		}

		public ClientToolMode DesignationToolMode
		{
			get { return (ClientToolMode)GetValue(DesignationToolModeProperty); }
			set { SetValue(DesignationToolModeProperty, value); }
		}

		public static readonly DependencyProperty DesignationToolModeProperty =
			DependencyProperty.Register("DesignationToolMode", typeof(ClientToolMode), typeof(MapToolBar), new UIPropertyMetadata(ClientToolMode.DesignationMine));


		private void Designation_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			var toolData = (ToolData)item.DataContext;

			this.DesignationToolMode = toolData.Mode;
			m_clientTools.ToolMode = toolData.Mode;
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
			m_clientTools.ToolMode = toolData.Mode;
		}

		private void RadioButton_Checked(object sender, RoutedEventArgs e)
		{
			var item = (RadioButton)sender;
			var toolData = (ToolData)item.DataContext;
			m_clientTools.ToolMode = toolData.Mode;
		}
	}
}
