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
using Dwarrowdelf;
using Dwarrowdelf.TerrainGen;

namespace TerrainGenTest
{
	public partial class MainWindow : Window
	{
		TerrainWriter m_terrain;

		public BitmapSource Bmp { get { return m_terrain.Bmp; } }

		public MainWindow()
		{
			m_terrain = new TerrainWriter();

			InitializeComponent();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			Render();
		}

		void Render()
		{
			if (!this.IsInitialized)
				return;

			double range = rangeSlider.Value;
			double h = hSlider.Value;
			int seed = ParseInt(seedTextBox.Text);

			var corners = new DiamondSquare.CornerData()
			{
				NW = ParseDouble(cornerNWTextBox.Text),
				NE = ParseDouble(cornerNETextBox.Text),
				SE = ParseDouble(cornerSETextBox.Text),
				SW = ParseDouble(cornerSWTextBox.Text),
			};

			m_terrain.Amplify = ParseInt(amplifyTextBox.Text);

			m_terrain.Generate(corners, range, h, seed);

			avgTextBox.Text = m_terrain.Average.ToString();
			//minTextBox.Text = m_terrain.Min.ToString();
			//maxTextBox.Text = m_terrain.Max.ToString();
		}

		private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var v = zoomSlider.Value;

			image.Width = m_terrain.Bmp.PixelWidth * v;
			image.Height = m_terrain.Bmp.PixelHeight * v;

		}

		private void hSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Render();
		}

		private void rangeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Render();
		}

		private void seedTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.IsInitialized)
				return;

			Render();
		}

		private void cornerTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.IsInitialized)
				return;

			Render();
		}

		int ParseInt(string str)
		{
			int r;
			if (int.TryParse(str, out r))
				return r;
			else
				return 0;
		}

		double ParseDouble(string str)
		{
			double r;
			if (double.TryParse(str, out r))
				return r;
			else
				return 0;
		}
	}
}
