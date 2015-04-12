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
using System.Diagnostics;

namespace FOVTest
{
	enum LOSAlgo
	{
		ShadowCastRecursive,
		ShadowCastRecursiveStrict,
		RayCastBresenhams,
		RayCastLerp,
	}

	public partial class MainWindow : Window
	{
		int m_visionRange = 15;
		int m_mapSize = 30;
		Grid2D<bool> m_blockerMap;
		Grid2D<bool> m_visionMap;
		double m_tileSize = 24;

		IntVector2 m_viewerLocation;

		Action<IntVector2, int, Grid2D<bool>, IntSize2, Func<IntVector2, bool>> m_algoDel;

		bool m_doPerfTest = false;

		public MainWindow()
		{
			m_blockerMap = new Grid2D<bool>(m_mapSize * 2 + 1, m_mapSize * 2 + 1);
			m_blockerMap.Origin = new IntVector2(m_mapSize, m_mapSize);

			m_visionMap = new Grid2D<bool>(m_visionRange * 2 + 1, m_visionRange * 2 + 1);
			m_visionMap.Origin = new IntVector2(m_visionRange, m_visionRange);

			m_viewerLocation = new IntVector2(m_mapSize, m_mapSize);

			m_blockerMap[-1, -4] = true;
			m_blockerMap[1, -4] = true;

			m_blockerMap[2, 8] = true;
			m_blockerMap[2, 9] = true;

			m_blockerMap[3, 0] = true;
			m_blockerMap[3, 1] = true;

			m_blockerMap[3, 4] = true;

			m_blockerMap[3, 7] = true;
			m_blockerMap[3, 8] = true;
			m_blockerMap[3, 9] = true;

			m_blockerMap[4, 7] = true;
			m_blockerMap[4, 8] = true;
			m_blockerMap[4, 9] = true;

			m_blockerMap[5, 7] = true;
			m_blockerMap[5, 8] = true;
			m_blockerMap[5, 9] = true;

			m_blockerMap.Origin = new IntVector2();

			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			var los = (LOSAlgo)losComboBox.SelectedItem;
			SelectAlgo(los);

			if (m_doPerfTest)
			{
				PerfTest();
				return;
			}

			grid.Width = m_visionMap.Width * m_tileSize;
			grid.Height = m_visionMap.Height * m_tileSize;

			for (int y = -m_visionRange; y <= m_visionRange; ++y)
			{
				for (int x = -m_visionRange; x <= m_visionRange; ++x)
				{
					var rect = new Rectangle()
					{
						Width = m_tileSize,
						Height = m_tileSize,
						Stroke = Brushes.Black,
						StrokeThickness = 1,
						Tag = new IntVector2(x, y),
					};

					rect.MouseDown += label_MouseDown;
					rect.MouseMove += rect_MouseMove;
					grid.Children.Add(rect);
					Canvas.SetLeft(rect, x * m_tileSize + m_visionRange * m_tileSize);
					Canvas.SetTop(rect, y * m_tileSize + m_visionRange * m_tileSize);
				}
			}

			Dispatcher.BeginInvoke(new Action(UpdateFOV), null);
		}

		void rect_MouseMove(object sender, MouseEventArgs e)
		{
			canvas.Children.Clear();

			var b = (Rectangle)sender;
			var v = (IntVector2)b.Tag;
			var p = new Point(v.X, v.Y);

			if (v == new IntVector2(0, 0))
				return;

			canvas.Children.Add(NewLine(new Point(0, 0), new Point(p.X, p.Y)));

			double dx, dy;

			dx = p.X >= 0 ? 0.5 : -0.5;
			dy = p.Y >= 0 ? 0.5 : -0.5;

			canvas.Children.Add(NewLine(new Point(0, 0), new Point(p.X - dx, p.Y + (p.X == 0 ? -dy : dy))));
			canvas.Children.Add(NewLine(new Point(0, 0), new Point(p.X + (p.Y == 0 ? -dx : dx), p.Y - dy)));
		}

		Line NewLine(Point p1, Point p2)
		{
			var ts = m_tileSize;

			// make the lines longer
			p2 = new Point(p2.X * 100, p2.Y * 100);

			var translatex = new Func<double, double>(v => (m_visionRange + v) * ts + 0.5 * ts);
			var translatey = new Func<double, double>(v => (m_visionRange + v) * ts + 0.5 * ts);

			var line = new Line()
			{
				Stroke = Brushes.Blue,
				StrokeThickness = 1,
				X1 = translatex(p1.X),
				Y1 = translatey(p1.X),
				X2 = translatex(p2.X),
				Y2 = translatey(p2.Y),
				IsHitTestVisible = false,
			};

			return line;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			var dir = KeyToDir(e.Key);

			if (dir == Direction.None)
			{
				base.OnKeyDown(e);
				return;
			}

			e.Handled = true;

			var l = m_viewerLocation + dir;

			if (m_blockerMap.Bounds.Contains(l) == false)
				return;

			m_viewerLocation += dir;

			UpdateFOV();
		}

		static Direction KeyToDir(Key key)
		{
			switch (key)
			{
				case Key.Up: return Direction.North;
				case Key.Down: return Direction.South;
				case Key.Left: return Direction.West;
				case Key.Right: return Direction.East;
				case Key.Home: return Direction.NorthWest;
				case Key.End: return Direction.SouthWest;
				case Key.PageUp: return Direction.NorthEast;
				case Key.PageDown: return Direction.SouthEast;
				default: return Direction.None;
			}
		}

		void label_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var b = (Rectangle)sender;
			var p = (IntVector2)b.Tag;

			p = p.Offset(m_viewerLocation.X, m_viewerLocation.Y);

			if (m_blockerMap.Bounds.Contains(p) == false)
				return;

			m_blockerMap[p] = !m_blockerMap[p];

			UpdateFOV();
		}

		void Calc(IntVector2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap, IntSize2 mapSize,
			Func<IntVector2, bool> blockerDelegate)
		{
			m_algoDel(viewerLocation, visionRange, visibilityMap, mapSize, blockerDelegate);
		}

		void PerfTest()
		{
			foreach (var algo in (LOSAlgo[])Enum.GetValues(typeof(LOSAlgo)))
			{
				SelectAlgo(algo);

				Calc(m_viewerLocation, m_visionRange, m_visionMap, m_blockerMap.Bounds.Size, p => m_blockerMap[p]);

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var sw = Stopwatch.StartNew();

				Calc(m_viewerLocation, m_visionRange, m_visionMap, m_blockerMap.Bounds.Size, p => m_blockerMap[p]);

				sw.Stop();
				Trace.TraceInformation("{1}: Elapsed {0} ms", sw.ElapsedMilliseconds, algo);
			}

			Application.Current.Shutdown();
		}

		void UpdateFOV()
		{
			Calc(m_viewerLocation, m_visionRange, m_visionMap, m_blockerMap.Bounds.Size, p => m_blockerMap[p]);

			foreach (Rectangle b in grid.Children)
			{
				var p = (IntVector2)b.Tag;

				if (p == new IntVector2())
				{
					b.Fill = Brushes.Red;
					continue;
				}

				var ml = p.Offset(m_viewerLocation.X, m_viewerLocation.Y);

				if (m_blockerMap.Bounds.Contains(ml) == false)
				{
					b.Fill = Brushes.DarkRed;
					continue;
				}

				var isBlocker = m_blockerMap[ml];
				var isVis = m_visionMap[p];

				if (isVis)
				{
					if (isBlocker)
						b.Fill = Brushes.LightGray;
					else
						b.Fill = Brushes.LightGreen;
				}
				else
				{
					if (isBlocker)
						b.Fill = Brushes.DimGray;
					else
						b.Fill = Brushes.DarkGreen;
				}
			}
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.IsInitialized == false)
				return;

			var cb = (ComboBox)sender;
			var los = (LOSAlgo)cb.SelectedItem;

			SelectAlgo(los);

			UpdateFOV();
		}

		void SelectAlgo(LOSAlgo los)
		{
			switch (los)
			{
				case LOSAlgo.ShadowCastRecursive:
					m_algoDel = ShadowCastRecursive.Calculate;
					break;

				case LOSAlgo.ShadowCastRecursiveStrict:
					m_algoDel = ShadowCastRecursiveStrict.Calculate;
					break;

				case LOSAlgo.RayCastBresenhams:
					m_algoDel = RayCastBresenhams.Calculate;
					break;

				case LOSAlgo.RayCastLerp:
					m_algoDel = RayCastLerp.Calculate;
					break;

				default:
					m_algoDel = null;
					break;
			}
		}
	}
}
