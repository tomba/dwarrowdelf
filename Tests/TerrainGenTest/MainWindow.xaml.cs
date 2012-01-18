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

			m_terrain.Generate(range, h);

			timeTextBox.Text = m_terrain.Time.TotalMilliseconds.ToString();
			minTextBox.Text = m_terrain.Min.ToString();
			maxTextBox.Text = m_terrain.Max.ToString();
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
	}
}
