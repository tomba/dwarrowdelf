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

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			m_needCreate = true;
			m_needGenerate = true;
			m_needRender = true;

			OnTimerTick(null, null);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			Vector v = new Vector();

			switch (e.Key)
			{
				case Key.Right:
					v = new Vector(1, 0);
					break;
				case Key.Left:
					v = new Vector(-1, 0);
					break;
				case Key.Up:
					v = new Vector(0, -1);
					break;
				case Key.Down:
					v = new Vector(0, 1);
					break;
			}

			if (v != new Vector())
			{
				Point p = Win32.GetCursorPos();
				p += v;
				Win32.SetCursorPos(p);
			}

			base.OnKeyDown(e);
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

				int depth = this.Depth;
				int side = this.Side;

				m_size = new IntSize3(side, side, depth);

				this.X = side / 2;
				this.Y = side / 2;
				this.Z = depth;

				//m_terrain = new TerrainData(m_size);
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

				var random = new Random(1);

				m_terrain = NoiseTerrainGen.CreateNoiseTerrain(m_size, random);

				sw.Stop();

				Trace.TraceInformation("Generate took {0} ms", sw.ElapsedMilliseconds);

				m_needGenerate = false;
			}

			if (m_needRender)
			{
				Stopwatch sw = Stopwatch.StartNew();

				this.Renderer.ShowWaterEnabled = this.ShowWaterEnabled;
				this.Renderer.Render(m_terrain, new IntVector3(this.X, this.Y, this.Z));

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

		public static readonly DependencyProperty SideProperty =
			DependencyProperty.Register("Side", typeof(int), typeof(MainWindow), new PropertyMetadata(512, ReCreateTerrain));

		public int Depth
		{
			get { return (int)GetValue(DepthProperty); }
			set { SetValue(DepthProperty, value); }
		}

		public static readonly DependencyProperty DepthProperty =
			DependencyProperty.Register("Depth", typeof(int), typeof(MainWindow), new PropertyMetadata(32, ReCreateTerrain));



		public bool ShowWaterEnabled
		{
			get { return (bool)GetValue(ShowWaterEnabledProperty); }
			set { SetValue(ShowWaterEnabledProperty, value); }
		}

		public static readonly DependencyProperty ShowWaterEnabledProperty =
			DependencyProperty.Register("ShowWaterEnabled", typeof(bool), typeof(MainWindow),
			new PropertyMetadata(true, ReRenderTerrain));


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

		public double NWCorner
		{
			get { return (double)GetValue(NWCornerProperty); }
			set { SetValue(NWCornerProperty, value); }
		}

		public static readonly DependencyProperty NWCornerProperty =
			DependencyProperty.Register("NWCorner", typeof(double), typeof(MainWindow),
			new PropertyMetadata(10.0, ReGenerateTerrain));

		public double NECorner
		{
			get { return (double)GetValue(NECornerProperty); }
			set { SetValue(NECornerProperty, value); }
		}

		public static readonly DependencyProperty NECornerProperty =
			DependencyProperty.Register("NECorner", typeof(double), typeof(MainWindow),
			new PropertyMetadata(15.0, ReGenerateTerrain));

		public double SECorner
		{
			get { return (double)GetValue(SECornerProperty); }
			set { SetValue(SECornerProperty, value); }
		}

		public static readonly DependencyProperty SECornerProperty =
			DependencyProperty.Register("SECorner", typeof(double), typeof(MainWindow),
			new PropertyMetadata(10.0, ReGenerateTerrain));

		public double SWCorner
		{
			get { return (double)GetValue(SWCornerProperty); }
			set { SetValue(SWCornerProperty, value); }
		}

		public static readonly DependencyProperty SWCornerProperty =
			DependencyProperty.Register("SWCorner", typeof(double), typeof(MainWindow),
			new PropertyMetadata(10.0, ReGenerateTerrain));

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

		void UpdatePos(IntVector3 pos)
		{
			if (pos.X < m_size.Width)
				this.X = pos.X;

			if (pos.Y < m_size.Height)
				this.Y = pos.Y;

			if (pos.Z >= 0 && pos.Z < m_size.Depth)
				this.Z = pos.Z;
		}

		void UpdateTileInfo(IntVector3 p)
		{
			if (m_terrain.Size.Plane.Contains(p.ToIntVector2()) == false)
				return;

			int h = m_terrain.GetSurfaceLevel(p.ToIntVector2());

			zTextBlock.Text = String.Format("{0} ({1})", p, h);

			IntVector3 mp;

			if (p.Z == m_size.Depth)
				mp = new IntVector3(p.X, p.Y, h);
			else if (p.Z >= 0)
				mp = p;
			else
				return;

			terrainTextBlock.Text = m_terrain.GetTileID(mp).ToString();
			terrainMatTextBlock.Text = m_terrain.GetMaterialID(mp).ToString();

			//interiorTextBlock.Text = m_terrain.GetInteriorID(mp).ToString();
			//interiorMatTextBlock.Text = m_terrain.GetInteriorMaterialID(mp).ToString();
		}

		private void imageXY_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdatePos(new IntVector3((int)Math.Round(p.X), (int)Math.Round(p.Y), this.Z));
		}

		private void imageXY_MouseMove(object sender, MouseEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			var mp = new IntVector3((int)Math.Round(p.X), (int)Math.Round(p.Y), this.Z);

			UpdateTileInfo(mp);

			if (e.LeftButton == MouseButtonState.Released)
				return;

			UpdatePos(mp);
		}

		private void imageXZ_MouseMove(object sender, MouseEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			var mp = new IntVector3((int)Math.Round(p.X), this.Y, m_size.Depth - (int)Math.Round(p.Y) - 1);

			UpdateTileInfo(mp);

			if (e.LeftButton == MouseButtonState.Released)
				return;

			UpdatePos(mp);
		}

		private void imageYZ_MouseMove(object sender, MouseEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			var mp = new IntVector3(this.X, (int)Math.Round(p.Y), m_size.Depth - (int)Math.Round(p.X) - 1);

			UpdateTileInfo(mp);

			if (e.LeftButton == MouseButtonState.Released)
				return;

			UpdatePos(mp);
		}

		private void imageXZ_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdatePos(new IntVector3((int)Math.Round(p.X), this.Y, m_size.Depth - (int)Math.Round(p.Y) - 1));
		}

		private void imageYZ_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var img = (Image)sender;
			var p = e.GetPosition(img);

			UpdatePos(new IntVector3(this.X, (int)Math.Round(p.Y), m_size.Depth - (int)Math.Round(p.X) - 1));
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
