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
using System.ComponentModel;
using Dwarrowdelf.Client.TileControlD2D;
using System.Diagnostics;
using System.Windows.Threading;

namespace TileControlD2DTest
{
	public partial class MainWindow : Window, IRenderViewRenderer
	{
		public IntPoint MousePos
		{
			get { return (IntPoint)GetValue(MousePosProperty); }
			set { SetValue(MousePosProperty, value); }
		}

		public static readonly DependencyProperty MousePosProperty =
			DependencyProperty.Register("MousePos", typeof(IntPoint), typeof(MainWindow), null);


		public IntPoint ScreenLoc
		{
			get { return (IntPoint)GetValue(ScreenLocProperty); }
			set { SetValue(ScreenLocProperty, value); }
		}

		public static readonly DependencyProperty ScreenLocProperty =
			DependencyProperty.Register("ScreenLoc", typeof(IntPoint), typeof(MainWindow), null);


		RenderMap m_renderMap = new RenderMap();
		BitmapGen m_bitmapGen = new BitmapGen();

		Rectangle m_box;

		DispatcherTimer m_timer;

		public MainWindow()
		{
			InitializeComponent();

			System.Diagnostics.Debug.Listeners.Clear();
			System.Diagnostics.Debug.Listeners.Add(new MMLogTraceListener());
			System.Threading.Thread.CurrentThread.Name = "Main";

			m_bitmapGen.SetTileSize(32);

			tc.RenderView = this;
			tc.BitmapGenerator = m_bitmapGen;
			tc.TileSize = 32;
			tc.SizeChanged += OnTileControlSizeChanged;

			CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
			m_timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnTimerCallback, this.Dispatcher);
		}

		int m_fpsCounter;
		void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			m_fpsCounter++;
		}

		void OnTimerCallback(object ob, EventArgs args)
		{
			fpsTextBlock.Text = ((double)m_fpsCounter).ToString();
			m_fpsCounter = 0;
		}

		void OnTileControlSizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLine("MainWindow: OnTileControlSizeChanged({0})", e.NewSize);
			ResetBox();
		}


		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_box = new Rectangle()
			{
				Fill = Brushes.Blue,
				Opacity = 0.5,
			};

			canvas.Children.Add(m_box);
		}

		void ResetBox()
		{
			var p = tc.ScreenLocationToScreenPoint(new IntPoint(4, 4));
			p = tc.TranslatePoint(p, canvas);

			Canvas.SetLeft(m_box, p.X);
			Canvas.SetTop(m_box, p.Y);
			m_box.Width = 32;
			m_box.Height = 32;
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);

			if (e.Delta > 0)
				tc.TileSize *= 2;
			else
				tc.TileSize /= 2;
			m_bitmapGen.SetTileSize(tc.TileSize);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			var pos = e.GetPosition(tc);

			this.MousePos = new IntPoint((int)Math.Round(pos.X), (int)Math.Round(pos.Y));
			this.ScreenLoc = tc.ScreenPointToScreenLocation(pos);
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			tc.RequestRender();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			tc.RequestRender();
			ResetBox();
		}

		#region IRenderViewRenderer Members

		public RenderMap GetRenderMap(int columns, int rows)
		{
			Debug.WriteLine("GetRenderMap({0}, {1})", columns, rows);

			m_renderMap.Size = new IntSize(columns, rows);

			m_renderMap.ArrayGrid.Grid[rows / 2, columns / 2].Floor.SymbolID = SymbolID.Floor;

			return m_renderMap;
		}

		#endregion
	}

	class BitmapGen : Dwarrowdelf.Client.TileControlD2D.IBitmapGenerator
	{
		int m_tileSize;

		public void SetTileSize(int tileSize)
		{
			m_tileSize = tileSize;
		}

		public BitmapSource GetBitmap(SymbolID symbolID, GameColor color)
		{
			var dv = new DrawingVisual();
			var dc = dv.RenderOpen();
			dc.DrawEllipse(Brushes.Red, new Pen(Brushes.Blue, 2), new Point(m_tileSize / 2, m_tileSize / 2), m_tileSize / 2, m_tileSize / 2);
			dc.Close();

			var bmp = new RenderTargetBitmap(m_tileSize, m_tileSize, 96, 96, PixelFormats.Default);
			bmp.Render(dv);

			return bmp;
		}

		public int NumDistinctBitmaps
		{
			get { return 2; }
		}

		public int TileSize
		{
			get
			{
				return m_tileSize;
			}
			set
			{
				throw new NotImplementedException();
			}
		}
	}
}
