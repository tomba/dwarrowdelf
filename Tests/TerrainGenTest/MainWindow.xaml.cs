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
		Generator m_terrain;
		Renderer m_renderer;

		public BitmapSource SliceBmpXY { get; private set; }
		public BitmapSource SliceBmpXZ { get; private set; }
		public BitmapSource SliceBmpYZ { get; private set; }

		IntSize3 m_size;
		IntPoint2 m_pos;

		public MainWindow()
		{
			const int depth = 20;
			const int sizeExp = 9;
			int size = (int)Math.Pow(2, sizeExp) + 1;

			m_size = new IntSize3(size, size, depth);
			m_terrain = new Generator(m_size);
			m_renderer = new Renderer(m_size);

			this.SliceBmpXY = m_renderer.SliceBmpXY;
			this.SliceBmpXZ = m_renderer.SliceBmpXZ;
			this.SliceBmpYZ = m_renderer.SliceBmpYZ;

			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			levelSlider.Minimum = 0;
			levelSlider.Maximum = m_size.Depth;
			levelSlider.Value = levelSlider.Maximum;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			Generate();
			Render();
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

		void Render()
		{
			if (!this.IsInitialized)
				return;

			m_renderer.Render(m_terrain.HeightMap, m_terrain.TileGrid, (int)levelSlider.Value, m_pos);
		}

		private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			/*
			var v = surfaceZoomSlider.Value;
			int m = (int)Math.Pow(2, v - 1);

			surfaceImage.Width = this.SurfaceBmp.PixelWidth * m;
			surfaceImage.Height = this.SurfaceBmp.PixelHeight * m;
			 */
		}

		private void levelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Render();
		}

		private void hSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Generate();
			Render();
		}

		private void rangeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Generate();
			Render();
		}

		private void seedTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.IsInitialized)
				return;

			Generate();
			Render();
		}

		private void cornerTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.IsInitialized)
				return;

			Generate();
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

		private void imageXY_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			var pos = new IntPoint2((int)Math.Round(p.X), (int)Math.Round(p.Y));

			if (m_size.Plane.Contains(pos) == false)
				return;

			m_pos = pos;

			Render();
		}
	}
}
