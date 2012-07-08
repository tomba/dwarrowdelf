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
using System.Windows.Threading;
using System.Diagnostics;

namespace TerrainGenTest
{
	public partial class MainWindow : Window
	{
		TerrainGenerator m_terrain;
		Renderer m_renderer;

		public BitmapSource SliceBmpXY { get; private set; }
		public BitmapSource SliceBmpXZ { get; private set; }
		public BitmapSource SliceBmpYZ { get; private set; }

		IntSize3 m_size;

		DispatcherTimer m_timer;

		bool m_needGenerate;
		bool m_needRender;

		public MainWindow()
		{
			const int depth = 20;
			const int sizeExp = 9;
			int size = (int)Math.Pow(2, sizeExp);

			m_size = new IntSize3(size, size, depth);
			m_terrain = new TerrainGenerator(m_size, new Random(1));
			m_renderer = new Renderer(m_size);

			this.SliceBmpXY = m_renderer.SliceBmpXY;
			this.SliceBmpXZ = m_renderer.SliceBmpXZ;
			this.SliceBmpYZ = m_renderer.SliceBmpYZ;

			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_timer = new DispatcherTimer();
			m_timer.Interval = TimeSpan.FromMilliseconds(20);
			m_timer.Tick += new EventHandler(OnTimerTick);

			levelSlider.Minimum = 0;
			levelSlider.Maximum = m_size.Depth;
			this.Z = m_size.Depth;

			Generate();
			Render();
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			string text = e.Text;

			e.Handled = true;

			if (text == ">")
			{
				if (this.Z > 0)
					this.Z--;
			}
			else if (text == "<")
			{
				if (this.Z < m_size.Depth)
					this.Z++;
			}
			else
			{
				e.Handled = false;
			}

			base.OnTextInput(e);
		}

		void OnTimerTick(object sender, EventArgs e)
		{
			m_timer.IsEnabled = false;

			if (m_needGenerate)
				Generate();

			if (m_needRender)
				Render();

			m_needGenerate = false;
			m_needRender = false;
		}


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



		public int Z
		{
			get { return (int)GetValue(ZProperty); }
			set { SetValue(ZProperty, value); }
		}

		public static readonly DependencyProperty ZProperty =
			DependencyProperty.Register("Z", typeof(int), typeof(MainWindow), new UIPropertyMetadata(0, ReRender));

		public int X
		{
			get { return (int)GetValue(XProperty); }
			set { SetValue(XProperty, value); }
		}

		public static readonly DependencyProperty XProperty =
			DependencyProperty.Register("X", typeof(int), typeof(MainWindow), new UIPropertyMetadata(0, ReRender));

		public int Y
		{
			get { return (int)GetValue(YProperty); }
			set { SetValue(YProperty, value); }
		}

		public static readonly DependencyProperty YProperty =
			DependencyProperty.Register("Y", typeof(int), typeof(MainWindow), new UIPropertyMetadata(0, ReRender));


		static void ReGenerate(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mw = (MainWindow)d;

			mw.m_needGenerate = true;
			mw.m_needRender = true;

			if (mw.m_timer.IsEnabled == false)
				mw.m_timer.IsEnabled = true;
		}

		static void ReRender(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mw = (MainWindow)d;

			mw.m_needRender = true;

			if (mw.m_timer.IsEnabled == false)
				mw.m_timer.IsEnabled = true;
		}

		private void cornerTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.IsInitialized)
				return;

			Generate();
			Render();
		}




		void Generate()
		{
			var corners = new DiamondSquare.CornerData()
			{
				NW = ParseDouble(cornerNWTextBox.Text),
				NE = ParseDouble(cornerNETextBox.Text),
				SE = ParseDouble(cornerSETextBox.Text),
				SW = ParseDouble(cornerSWTextBox.Text),
			};

			m_terrain.Generate(corners, this.RangeValue, this.HValue, this.Seed, this.Amplify);

			avgTextBox.Text = m_terrain.HeightMap.Average().ToString();
		}

		void Render()
		{
			m_renderer.Render(m_terrain.HeightMap, m_terrain.TileGrid, new IntPoint3(this.X, this.Y, this.Z));
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

		void UpdatePos(IntPoint3 pos)
		{
			if (pos.X < m_size.Width)
				this.X = pos.X;

			if (pos.Y < m_size.Height)
				this.Y = pos.Y;

			if (pos.Z < m_size.Depth)
				this.Z = pos.Z;
		}

		private void imageXY_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdatePos(new IntPoint3((int)Math.Round(p.X), (int)Math.Round(p.Y), this.Z));
		}

		private void imageXY_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Released)
				return;

			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdatePos(new IntPoint3((int)Math.Round(p.X), (int)Math.Round(p.Y), this.Z));
		}

		private void imageXZ_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdatePos(new IntPoint3((int)Math.Round(p.X), this.Y, m_size.Depth - (int)Math.Round(p.Y) - 1));
		}

		private void imageYZ_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdatePos(new IntPoint3(this.X, (int)Math.Round(p.Y), m_size.Depth - (int)Math.Round(p.X) - 1));
		}

		private void mapGrid_MouseMove(object sender, MouseEventArgs e)
		{
			var v = VisualTreeHelper.GetOffset(mapGrid);
			var pos = e.GetPosition(mapGrid);
			var vb = magnifierBrush.Viewbox;
			vb.X = pos.X - vb.Width / 2 + v.X;
			vb.Y = pos.Y - vb.Height / 2 + v.Y;
			magnifierBrush.Viewbox = vb;
		}

		private void scrollViewerXY_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			scrollViewerXZ.ScrollToHorizontalOffset(((ScrollViewer)sender).HorizontalOffset);
			scrollViewerYZ.ScrollToVerticalOffset(((ScrollViewer)sender).VerticalOffset);
		}

	}
}
