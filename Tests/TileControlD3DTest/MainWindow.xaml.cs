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
		double m_targetTileSize;
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

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			line1.X1 = 0;
			line1.X2 = 0;
			line1.Y1 = 0;
			line1.Y2 = tileControl.ActualHeight;
			Canvas.SetTop(line1, 0);
			Canvas.SetLeft(line1, tileControl.ActualWidth / 2);

			line2.X1 = 0;
			line2.X2 = tileControl.ActualWidth;
			line2.Y1 = 0;
			line2.Y2 = 0;
			Canvas.SetLeft(line2, 0);
			Canvas.SetBottom(line2, tileControl.ActualHeight / 2);
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
				case Key.Add:
					tileControl.TileSize += 1;
					return;

				case Key.Subtract:
					tileControl.TileSize -= 1;
					return;

				default:
					var diff = new IntVector(-10, -10);

					var anim2 = new PointAnimation(tileControl.CenterPos - new Vector(diff.X, diff.Y), new Duration(TimeSpan.FromMilliseconds(2000)));
					this.BeginAnimation(TileControlD3D.CenterPosProperty, anim2);

					return;
			}

			//this.CenterPos += (v * 4);
			tileControl.CenterPos += new Vector(v.X, v.Y) / 10;
		}

		void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
		{
#if !old
			var origTileSize = m_targetTileSize;

			var targetTileSize = origTileSize;

			if (e.Delta > 0)
				targetTileSize *= 2;
			else
				targetTileSize /= 2;

			if (targetTileSize < 2)
				targetTileSize = 2;

			if (origTileSize == targetTileSize)
				return;

			m_targetTileSize = targetTileSize;


			var origCenter = tileControl.CenterPos;

			var p = e.GetPosition(tileControl);

			Vector v = p - new Point(tileControl.ActualWidth / 2, tileControl.ActualHeight / 2);
			v /= targetTileSize;
			v = new Vector(Math.Round(v.X), -Math.Round(v.Y));

			var ml = tileControl.ScreenPointToMapLocation(p);
			ml = new Point(Math.Round(ml.X), Math.Round(ml.Y));
			var targetCenter = ml - v;

			targetCenter = new Point(Math.Round(targetCenter.X), Math.Round(targetCenter.Y));

			var anim = new DoubleAnimation(targetTileSize, new Duration(TimeSpan.FromMilliseconds(200)));
			tileControl.BeginAnimation(TileControlD3D.TileSizeProperty, anim);

			var anim2 = new PointAnimation(targetCenter, new Duration(TimeSpan.FromMilliseconds(200)));
			if (e.Delta > 0)
				anim2.DecelerationRatio = 1.0;
			else
				anim2.AccelerationRatio = 1.0;

			tileControl.BeginAnimation(TileControlD3D.CenterPosProperty, anim2);

			Debug.Print("Anim Size {0:F2} -> {1:F2}, Center {2:F2} -> {3:F2}", origTileSize, targetTileSize, origCenter, targetCenter);

#else
			var targetTileSize = tileControl.TileSize;

			if (e.Delta > 0)
				targetTileSize *= 2;
			else
				targetTileSize /= 2;

			if (targetTileSize < 2)
				targetTileSize = 2;


			var p = e.GetPosition(tileControl);

			Vector v = p - new Point(tileControl.ActualWidth / 2, tileControl.ActualHeight / 2);
			v /= targetTileSize;
			v = new Vector(Math.Round(v.X), -Math.Round(v.Y));

			var ml = ScreenPointToMapLocation(p);
			ml = new Point(Math.Round(ml.X), Math.Round(ml.Y));
			var targetCenter = ml - v;
			targetCenter = new Point(Math.Round(targetCenter.X), Math.Round(targetCenter.Y));

			this.CenterPos = targetCenter;
			tileControl.TileSize = targetTileSize;
#endif
		}



		protected override void OnMouseMove(MouseEventArgs e)
		{
			var sp = e.GetPosition(tileControl);
			var sl = tileControl.ScreenPointToScreenLocation(sp);
			var ml = tileControl.ScreenLocationToMapLocation(sl);

			mapLocationTextBox.Text = String.Format("{0:F2}, {1:F2}", ml.X, ml.Y);
			screenLocationTextBox.Text = String.Format("{0:F2}, {1:F2}", sl.X, sl.Y);

			mapLocationITextBox.Text = String.Format("{0:F0}, {1:F0}", ml.X, ml.Y);
			screenLocationITextBox.Text = String.Format("{0:F0}, {1:F0}", sl.X, sl.Y);

			base.OnMouseMove(e);
		}

		void tileControl_TileArrangementChanged(IntSize gridSize)
		{
			m_renderData.Size = gridSize;

			rect.Width = tileControl.TileSize;
			rect.Height = tileControl.TileSize;
			var mp = tileControl.MapLocationToScreenLocation(new Point(5, 5));
			var p = tileControl.ScreenLocationToScreenPoint(mp);
			Canvas.SetLeft(rect, p.X);
			Canvas.SetTop(rect, p.Y);

		}

		void tileControl_AboutToRender()
		{
			var arr = m_renderData.ArrayGrid.Grid;

			Array.Clear(arr, 0, arr.Length);

			foreach (var sp in m_renderData.Bounds.Range())
			{
				var mp = tileControl.ScreenLocationToMapLocation(new Point(sp.X, sp.Y));

				var x = sp.X;
				var y = sp.Y;
				int mx = (int)Math.Round(mp.X);
				int my = (int)Math.Round(mp.Y);

				if (mx < 0 || my < 0 || mx >= 128 || my >= 128)
					continue;

				if (mx == my)
				{
					arr[y, x].FloorSymbolID = SymbolID.Grass;
					arr[y, x].FloorColor = GameColor.None;
					arr[y, x].InteriorSymbolID = (SymbolID)((mx % 10) + 1);
					arr[y, x].InteriorColor = (GameColor)((mx % ((int)GameColor.NumColors - 1)) + 1);
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
