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

namespace MyGame
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	partial class MainWindow : Window
	{
		public static MainWindow s_mainWindow; // xxx

		public MainWindow()
		{
			s_mainWindow = this;

			InitializeComponent();

			this.Width = 1024;
			this.Height = 700;
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			GameData.Data.MyTraceListener.TextBox = this.logTextBox;

			map.Focus();
		}

		internal MapLevel Map
		{
			get
			{
				return this.map.MapLevel;
			}

			set
			{
				this.map.MapLevel = value;
			}
		}

		private void OnClearLogClicked(object sender, RoutedEventArgs e)
		{
			this.logTextBox.Clear();
		}
	
	}

}
