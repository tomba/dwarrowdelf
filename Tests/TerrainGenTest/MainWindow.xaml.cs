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

		public BitmapSource SurfaceBmp { get; private set; }
		public BitmapSource SliceBmp { get; private set; }

		public MainWindow()
		{
			m_terrain = new TerrainWriter();
			this.SurfaceBmp = m_terrain.SurfaceBmp;
			this.SliceBmp = m_terrain.SliceBmp;

			InitializeComponent();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			Generate();
			RenderTerrain();
			RenderSlice();
		}

		void Generate()
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

		void RenderTerrain()
		{
			m_terrain.RenderTerrain();
		}

		void RenderSlice()
		{
			m_terrain.Level = (int)levelSlider.Value;

			m_terrain.RenderSlice();
		}

		private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var v = surfaceZoomSlider.Value;
			int m = (int)Math.Pow(2, v - 1);

			surfaceImage.Width = this.SurfaceBmp.PixelWidth * m;
			surfaceImage.Height = this.SurfaceBmp.PixelHeight * m;
		}

		private void sliceZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var v = sliceZoomSlider.Value;
			int m = (int)Math.Pow(2, v - 1);

			sliceImage.Width = this.SliceBmp.PixelWidth * m;
			sliceImage.Height = this.SliceBmp.PixelHeight * m;
		}

		private void levelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			RenderSlice();
		}

		private void hSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Generate();
			RenderTerrain();
			RenderSlice();
		}

		private void rangeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Generate();
			RenderTerrain();
			RenderSlice();
		}

		private void seedTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.IsInitialized)
				return;

			Generate();
			RenderTerrain();
			RenderSlice();
		}

		private void cornerTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.IsInitialized)
				return;

			Generate();
			RenderTerrain();
			RenderSlice();
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
