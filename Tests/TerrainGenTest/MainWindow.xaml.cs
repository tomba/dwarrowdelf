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
using System.ComponentModel;

namespace TerrainGenTest
{
	partial class MainWindow : Window, INotifyPropertyChanged
	{
		TerrainData m_terrain;
		TerrainGenerator m_terrainGen;
		public Renderer Renderer { get; private set; }

		IntSize3 m_size;

		DispatcherTimer m_timer;

		bool m_needCreate;
		bool m_needGenerate;
		bool m_needRender;

		public MainWindow()
		{
			m_timer = new DispatcherTimer();
			m_timer.Interval = TimeSpan.FromMilliseconds(20);
			m_timer.Tick += new EventHandler(OnTimerTick);

			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_needCreate = true;
			m_needGenerate = true;
			m_needRender = true;

			OnTimerTick(null, null);
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

		void StartCreateTerrain()
		{
			m_needCreate = true;
			m_needGenerate = true;
			m_needRender = true;

			if (m_timer.IsEnabled == false)
				m_timer.IsEnabled = true;
		}

		void StartGenerateTerrain()
		{
			m_needGenerate = true;
			m_needRender = true;

			if (m_timer.IsEnabled == false)
				m_timer.IsEnabled = true;
		}

		void StartRenderTerrain()
		{
			m_needRender = true;

			if (m_timer.IsEnabled == false)
				m_timer.IsEnabled = true;
		}

		void OnTimerTick(object sender, EventArgs e)
		{
			m_timer.IsEnabled = false;

			if (m_needCreate)
			{
				Stopwatch sw = Stopwatch.StartNew();

				const int depth = 10;
				const int sizeExp = 9;
				int side = (int)Math.Pow(2, sizeExp);

				m_size = new IntSize3(side, side, depth);
				m_terrain = new TerrainData(m_size);
				//m_terrainGen = new DungeonTerrainGenerator(m_terrain, new Random(1));
				m_terrainGen = new TerrainGenerator(m_terrain, new Random(1));
				this.Renderer = new Renderer(m_size);
				Notify("Renderer");

				sw.Stop();

				Trace.TraceInformation("Create took {0} ms", sw.ElapsedMilliseconds);

				levelSlider.Minimum = 0;
				levelSlider.Maximum = m_size.Depth;
				this.Z = m_size.Depth;

				m_needCreate = false;
			}

			if (m_needGenerate)
			{
				Stopwatch sw = Stopwatch.StartNew();

				var corners = new DiamondSquare.CornerData()
				{
					NW = ParseDouble(cornerNWTextBox.Text),
					NE = ParseDouble(cornerNETextBox.Text),
					SE = ParseDouble(cornerSETextBox.Text),
					SW = ParseDouble(cornerSWTextBox.Text),
				};

				m_terrainGen.Generate(corners, this.RangeValue, this.HValue, this.Seed, this.Amplify);
				//m_terrainGen.Generate(this.Seed);

				sw.Stop();

				Trace.TraceInformation("Generate took {0} ms", sw.ElapsedMilliseconds);

				m_needGenerate = false;
			}

			if (m_needRender)
			{
				Stopwatch sw = Stopwatch.StartNew();

				this.Renderer.Render(m_terrain, new IntPoint3(this.X, this.Y, this.Z));

				sw.Stop();

				Trace.TraceInformation("Render took {0} ms", sw.ElapsedMilliseconds);

				m_needRender = false;
			}

		}



		public int Side
		{
			get { return (int)GetValue(SideProperty); }
			set { SetValue(SideProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Side.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SideProperty =
			DependencyProperty.Register("Side", typeof(int), typeof(MainWindow), new PropertyMetadata(512, ReCreateTerrain));



		public double Amplify
		{
			get { return (double)GetValue(AmplifyProperty); }
			set { SetValue(AmplifyProperty, value); }
		}

		public static readonly DependencyProperty AmplifyProperty =
			DependencyProperty.Register("Amplify", typeof(double), typeof(MainWindow),
				new UIPropertyMetadata(2.0, ReGenerateTerrain));

		public double HValue
		{
			get { return (double)GetValue(HValueProperty); }
			set { SetValue(HValueProperty, value); }
		}

		public static readonly DependencyProperty HValueProperty =
			DependencyProperty.Register("HValue", typeof(double), typeof(MainWindow),
			new UIPropertyMetadata(0.75, ReGenerateTerrain));

		public double RangeValue
		{
			get { return (double)GetValue(RangeValueProperty); }
			set { SetValue(RangeValueProperty, value); }
		}

		public static readonly DependencyProperty RangeValueProperty =
			DependencyProperty.Register("RangeValue", typeof(double), typeof(MainWindow),
			new UIPropertyMetadata(5.0, ReGenerateTerrain));

		public int Seed
		{
			get { return (int)GetValue(SeedProperty); }
			set { SetValue(SeedProperty, value); }
		}

		public static readonly DependencyProperty SeedProperty =
			DependencyProperty.Register("Seed", typeof(int), typeof(MainWindow), new UIPropertyMetadata(1, ReGenerateTerrain));



		public int Z
		{
			get { return (int)GetValue(ZProperty); }
			set { SetValue(ZProperty, value); }
		}

		public static readonly DependencyProperty ZProperty =
			DependencyProperty.Register("Z", typeof(int), typeof(MainWindow), new UIPropertyMetadata(0, ReRenderTerrain));

		public int X
		{
			get { return (int)GetValue(XProperty); }
			set { SetValue(XProperty, value); }
		}

		public static readonly DependencyProperty XProperty =
			DependencyProperty.Register("X", typeof(int), typeof(MainWindow), new UIPropertyMetadata(0, ReRenderTerrain));

		public int Y
		{
			get { return (int)GetValue(YProperty); }
			set { SetValue(YProperty, value); }
		}

		public static readonly DependencyProperty YProperty =
			DependencyProperty.Register("Y", typeof(int), typeof(MainWindow), new UIPropertyMetadata(0, ReRenderTerrain));

		static void ReCreateTerrain(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mw = (MainWindow)d;
			mw.StartCreateTerrain();
		}

		static void ReGenerateTerrain(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mw = (MainWindow)d;
			mw.StartGenerateTerrain();
		}

		static void ReRenderTerrain(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mw = (MainWindow)d;
			mw.StartRenderTerrain();
		}

		private void cornerTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.IsInitialized)
				return;

			StartGenerateTerrain();
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

		void UpdateTileInfo(IntPoint3 p)
		{
			if (m_terrain.Contains(p) == false)
				return;

			int h = m_terrain.GetHeight(p.ToIntPoint());

			zTextBlock.Text = String.Format("{0}/{1}", p, h);

			IntPoint3 mp;

			if (p.Z == m_size.Depth)
				mp = new IntPoint3(p.X, p.Y, h);
			else if (p.Z >= 0)
				mp = p;
			else
				return;

			var terrainMat = m_terrain.GetTerrainMaterialID(mp);
			var interiorMat = m_terrain.GetInteriorMaterialID(mp);

			if (terrainMat == MaterialID.Undefined)
				materialTextBlock.Text = "";
			else
				materialTextBlock.Text = terrainMat.ToString();

			if (interiorMat == MaterialID.Undefined)
				oreTextBlock.Text = "";
			else
				oreTextBlock.Text = interiorMat.ToString();
		}

		private void imageXY_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdatePos(new IntPoint3((int)Math.Round(p.X), (int)Math.Round(p.Y), this.Z));
		}

		private void imageXY_MouseMove(object sender, MouseEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			var mp = new IntPoint3((int)Math.Round(p.X), (int)Math.Round(p.Y), this.Z);

			UpdateTileInfo(mp);

			if (e.LeftButton == MouseButtonState.Released)
				return;

			UpdatePos(mp);
		}

		private void imageXZ_MouseMove(object sender, MouseEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdateTileInfo(new IntPoint3((int)Math.Round(p.X), this.Y, m_size.Depth - (int)Math.Round(p.Y) - 1));
		}

		private void imageYZ_MouseMove(object sender, MouseEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdateTileInfo(new IntPoint3(this.X, (int)Math.Round(p.Y), m_size.Depth - (int)Math.Round(p.X) - 1));
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
			vb.X = pos.X - vb.Width / 2 + v.X + 0.5;
			vb.Y = pos.Y - vb.Height / 2 + v.Y + 0.5;
			magnifierBrush.Viewbox = vb;
		}

		private void scrollViewerXY_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			scrollViewerXZ.ScrollToHorizontalOffset(((ScrollViewer)sender).HorizontalOffset);
			scrollViewerYZ.ScrollToVerticalOffset(((ScrollViewer)sender).VerticalOffset);
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		protected void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
