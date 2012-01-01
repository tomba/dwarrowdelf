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
using Dwarrowdelf.Messages;

namespace Dwarrowdelf.Client.UI
{
	/// <summary>
	/// Interaction logic for MainWindowToolBar.xaml
	/// </summary>
	internal partial class MainWindowToolBar : UserControl
	{
		static MainWindowToolBar()
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
			add(ClientToolMode.ConstructBuilding, "Construct building", 'b');
		}

		public MainWindowToolBar()
		{
			InitializeComponent();
		}

		private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (GameData.Data.User != null)
			{
				GameData.Data.Connection.Send(new SetWorldConfigMessage()
				{
					MinTickTime = TimeSpan.FromMilliseconds(slider.Value),
				});
			}
		}

		private void Connect_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection != null)
				return;

			App.MainWindow.Connect();
		}

		private void Disconnect_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection == null)
				return;

			App.MainWindow.Disconnect();
		}


		private void EnterGame_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.User == null || GameData.Data.User.IsPlayerInGame)
				return;

			App.MainWindow.EnterGame();
		}

		private void ExitGame_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.User == null || !GameData.Data.User.IsPlayerInGame)
				return;

			App.MainWindow.ExitGame();
		}

		private void Save_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection == null)
				return;

			var msg = new SaveRequestMessage();

			GameData.Data.Connection.Send(msg);
		}

		private void Load_Button_Click(object sender, RoutedEventArgs e)
		{

		}

		private void Button_Click_FullScreen(object sender, RoutedEventArgs e)
		{
			var button = (System.Windows.Controls.Primitives.ToggleButton)sender;

			var wnd = App.MainWindow;

			if (button.IsChecked.Value)
			{
				wnd.WindowStyle = System.Windows.WindowStyle.None;
				wnd.Topmost = true;
				wnd.WindowState = System.Windows.WindowState.Maximized;
			}
			else
			{
				wnd.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
				wnd.Topmost = false;
				wnd.WindowState = System.Windows.WindowState.Normal;
			}
		}


		private void Button_OpenNetStats_Click(object sender, RoutedEventArgs e)
		{
			var netWnd = new UI.NetStatWindow();
			netWnd.Owner = App.MainWindow;
			netWnd.Show();
		}

		private void Button_Click_GC(object sender, RoutedEventArgs e)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		private void Button_Click_Break(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}


		private void Button_Click_EditSymbols(object sender, RoutedEventArgs e)
		{
			var dialog = new Dwarrowdelf.Client.Symbols.SymbolEditorDialog();
			dialog.SymbolDrawingCache = GameData.Data.SymbolDrawingCache;
			dialog.Show();
		}

		/**
		 * TOOLS
		 */


		public static readonly Dictionary<ClientToolMode, ToolData> ToolDatas;

		public event Action<ClientToolMode> ToolModeChanged;

		public ClientToolMode ToolMode
		{
			get { return (ClientToolMode)GetValue(ToolModeProperty); }
			set { SetValue(ToolModeProperty, value); }
		}

		public static readonly DependencyProperty ToolModeProperty =
			DependencyProperty.Register("ToolMode", typeof(ClientToolMode), typeof(MainWindowToolBar),
			new UIPropertyMetadata(new PropertyChangedCallback(ToolModeChangedCallback)));

		static void ToolModeChangedCallback(DependencyObject ob, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (MainWindowToolBar)ob;
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
			DependencyProperty.Register("DesignationToolMode", typeof(ClientToolMode), typeof(MainWindowToolBar), new UIPropertyMetadata(ClientToolMode.DesignationMine));


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

	sealed class ToolData
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

	sealed class ClientToolModeToToolDataConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return null;

			var mode = (ClientToolMode)value;

			var data = MainWindowToolBar.ToolDatas[mode];

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
		ConstructBuilding,
	}
}
