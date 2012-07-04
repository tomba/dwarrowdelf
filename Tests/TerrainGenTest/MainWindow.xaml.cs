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


		public double Amplify
		{
			get { return (double)GetValue(AmplifyProperty); }
			set { SetValue(AmplifyProperty, value); }
		}

		public static readonly DependencyProperty AmplifyProperty =
			DependencyProperty.Register("Amplify", typeof(double), typeof(MainWindow),
				new UIPropertyMetadata(2.0, ReGenerate));

		public double HValue
		{
			get { return (double)GetValue(HValueProperty); }
			set { SetValue(HValueProperty, value); }
		}

		public static readonly DependencyProperty HValueProperty =
			DependencyProperty.Register("HValue", typeof(double), typeof(MainWindow),
			new UIPropertyMetadata(0.75, ReGenerate));

		public double RangeValue
		{
			get { return (double)GetValue(RangeValueProperty); }
			set { SetValue(RangeValueProperty, value); }
		}

		public static readonly DependencyProperty RangeValueProperty =
			DependencyProperty.Register("RangeValue", typeof(double), typeof(MainWindow),
			new UIPropertyMetadata(5.0, ReGenerate));

		public int Seed
		{
			get { return (int)GetValue(SeedProperty); }
			set { SetValue(SeedProperty, value); }
		}

		public static readonly DependencyProperty SeedProperty =
			DependencyProperty.Register("Seed", typeof(int), typeof(MainWindow), new UIPropertyMetadata(1, ReGenerate));



		static void ReGenerate(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mw = (MainWindow)d;
			mw.Generate();
			mw.Render();
		}


		private void cornerTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.IsInitialized)
				return;

			Generate();
			Render();
		}




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

			var corners = new DiamondSquare.CornerData()
			{
				NW = ParseDouble(cornerNWTextBox.Text),
				NE = ParseDouble(cornerNETextBox.Text),
				SE = ParseDouble(cornerSETextBox.Text),
				SW = ParseDouble(cornerSWTextBox.Text),
			};

			m_terrain.Generate(corners, this.RangeValue, this.HValue, this.Seed, this.Amplify);

			avgTextBox.Text = m_terrain.Average.ToString();
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
