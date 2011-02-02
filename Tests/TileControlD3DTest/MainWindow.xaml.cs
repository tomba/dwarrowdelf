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
using Dwarrowdelf.Client;
using Dwarrowdelf.Client.TileControl;
using Dwarrowdelf;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Threading;

namespace TileControlD3DTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		RenderData<RenderTileDetailedD3D> m_renderData;
		int m_targetTileSize;
		IntPoint m_centerPos;
		DispatcherTimer m_timer;

		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			tileControl.AboutToRender += tileControl_AboutToRender;
			tileControl.TileLayoutChanged += tileControl_TileArrangementChanged;

			var symbolDrawingCache = new SymbolDrawingCache(new Uri("/Symbols/SymbolInfosGfx.xaml", UriKind.Relative));
			tileControl.SymbolDrawingCache = symbolDrawingCache;

			m_renderData = new RenderData<RenderTileDetailedD3D>();
			tileControl.SetRenderData(m_renderData);
			tileControl.TileSize = 32;
			m_targetTileSize = tileControl.TileSize;

			this.MouseWheel += new MouseWheelEventHandler(MainWindow_MouseWheel);
			this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);

			CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
			m_timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnTimerCallback, this.Dispatcher);
		}

		static Direction KeyToDir(Key key)
		{
			Direction dir;

			switch (key)
			{
				case Key.Up: dir = Direction.North; break;
				case Key.Down: dir = Direction.South; break;
				case Key.Left: dir = Direction.West; break;
				case Key.Right: dir = Direction.East; break;
				case Key.Home: dir = Direction.NorthWest; break;
				case Key.End: dir = Direction.SouthWest; break;
				case Key.PageUp: dir = Direction.NorthEast; break;
				case Key.PageDown: dir = Direction.SouthEast; break;
				default:
					throw new Exception();
			}

			return dir;
		}

		void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			IntVector v;

			switch (e.Key)
			{
				case Key.Up: v = new IntVector(0, 1); break;
				case Key.Down: v = new IntVector(0, -1); break;
				case Key.Left: v = new IntVector(-1, 0); break;
				case Key.Right: v = new IntVector(1, 0); break;
				default: return;
			}

			this.CenterPos += (v * 4);
		}

		void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
		{
#if old
			var ts = m_targetTileSize;

			if (e.Delta > 0)
				ts *= 2;
			else
				ts /= 2;

			if (ts < 2)
				ts = 2;

			m_targetTileSize = ts;

			var anim = new Int32Animation(ts, new Duration(TimeSpan.FromMilliseconds(200)));
			tileControl.BeginAnimation(TileControlD3D.TileSizeProperty, anim);
			//	tileControl.TileSize = ts;
#else

			var p = e.GetPosition(tileControl);
			var ml1 = ScreenPointToMapLocation(p);

			var ts = tileControl.TileSize;

			if (e.Delta > 0)
				ts *= 2;
			else
				ts /= 2;

			if (ts < 2)
				ts = 2;

			tileControl.TileSize = ts;

			var ml2 = ScreenPointToMapLocation(p);
			var d = ml2 - this.CenterPos;
			var l = ml1 - d;

			this.CenterPos = l;
#endif
		}

		IntPoint CenterPos
		{
			get
			{
				return m_centerPos;
			}

			set
			{
				m_centerPos = value;
				tileControl.InvalidateRender();
				tileControl_TileArrangementChanged(tileControl.GridSize); // XXX
			}
		}

		int Columns { get { return tileControl.GridSize.Width; } }
		int Rows { get { return tileControl.GridSize.Height; } }

		IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, this.Rows / 2); }
		}

		public IntPoint MapLocationToScreenLocation(IntPoint ml)
		{
			return new IntPoint(ml.X - this.TopLeftPos.X, -(ml.Y - this.TopLeftPos.Y));
		}

		public IntPoint ScreenLocationToMapLocation(IntPoint sl)
		{
			return new IntPoint(sl.X + this.TopLeftPos.X, -(sl.Y - this.TopLeftPos.Y));
		}
		/*

		public IntPoint MapLocationToScreenLocation(IntPoint ml)
		{
			return new IntPoint(ml.X, -ml.Y) - new IntVector(m_centerPos.X, -m_centerPos.Y) + new IntVector(m_renderData.Size.Width / 2, m_renderData.Size.Height / 2);
		}

		public IntPoint ScreenLocationToMapLocation(IntPoint sp)
		{
			return sp + new IntVector(m_centerPos.X, m_centerPos.Y) - new IntVector(m_renderData.Size.Width / 2, m_renderData.Size.Height / 2);
		}
		*/
		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var sl = ScreenPointToScreenLocation(p);
			return ScreenLocationToMapLocation(sl);
		}

		public Point MapLocationToScreenPoint(IntPoint ml)
		{
			var sl = MapLocationToScreenLocation(ml);
			return ScreenLocationToScreenPoint(sl);
		}

		public IntPoint ScreenPointToScreenLocation(Point p)
		{
			return tileControl.ScreenPointToScreenLocation(p);
		}

		public Point ScreenLocationToScreenPoint(IntPoint loc)
		{
			return tileControl.ScreenLocationToScreenPoint(loc);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			var sp = e.GetPosition(tileControl);
			var sl = ScreenPointToScreenLocation(sp);
			var ml = ScreenLocationToMapLocation(sl);

			mapLocationTextBox.Text = String.Format("{0}, {1}", ml.X, ml.Y);
			screenLocationTextBox.Text = String.Format("{0}, {1}", sl.X, sl.Y);

			base.OnMouseMove(e);
		}

		void tileControl_TileArrangementChanged(IntSize gridSize)
		{
			m_renderData.Size = gridSize;

			rect.Width = tileControl.TileSize;
			rect.Height = tileControl.TileSize;
			var mp = MapLocationToScreenLocation(new IntPoint(5, 5));
			var p = tileControl.ScreenLocationToScreenPoint(mp);
			Canvas.SetLeft(rect, p.X);
			Canvas.SetTop(rect, p.Y);
		}

		void tileControl_AboutToRender(Size renderSize)
		{
			var arr = m_renderData.ArrayGrid.Grid;

			Array.Clear(arr, 0, arr.Length);

			foreach (var sp in m_renderData.Bounds.Range())
			{
				var mp = ScreenLocationToMapLocation(sp);

				var x = sp.X;
				var y = sp.Y;

				if (mp.X < 0 || mp.Y < 0 || mp.X >= 128 || mp.Y >= 128)
					continue;

				if (mp.X == mp.Y)
				{
					arr[y, x].FloorSymbolID = SymbolID.Grass;
					arr[y, x].FloorColor = GameColor.None;
					arr[y, x].InteriorSymbolID = (SymbolID)((mp.X % 10) + 1);
					arr[y, x].InteriorColor = (GameColor)((mp.X % ((int)GameColor.NumColors - 1)) + 1);
				}
				else
				{
					arr[y, x].FloorSymbolID = SymbolID.Grass;
					arr[y, x].FloorColor = GameColor.None;
				}
				//arr[y, x].Interior.SymbolID = (SymbolID)(id + 1);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			tileControl.Dispose();

			base.OnClosed(e);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
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
	}
}
