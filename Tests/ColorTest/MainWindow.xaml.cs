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
using MyGame;

namespace ColorTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			for (int i = 1; i < (int)GameColor.NumColors; ++i)
			{
				GameColor gc = (GameColor)i;
				GameColorRGB gcrgb = new GameColorRGB(gc);
				var brush = new SolidColorBrush(Color.FromRgb(gcrgb.R, gcrgb.G, gcrgb.B));
				var fgbrush = new SolidColorBrush(Color.FromRgb((byte)(255 - gcrgb.R), (byte)(255 - gcrgb.G), (byte)(255 - gcrgb.B)));

				var label = new Label();
				label.Content = Enum.GetName(typeof(GameColor), gc);
				label.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
				label.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
				label.Background = brush;
				label.Foreground = fgbrush;
				grid.Children.Add(label);
			}
		}
	}
}
