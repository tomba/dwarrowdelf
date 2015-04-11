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
		RayCast,
	}

	public partial class MainWindow : Window
	{
		int m_visionRange = 15;
		int m_mapSize = 20;
		Grid2D<bool> m_blockerMap;
		Grid2D<bool> m_visionMap;
		double m_tileSize;

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

			m_tileSize = 16;

			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			var los = (LOSAlgo)losComboBox.SelectedItem;
			SelectAlgo(los);

			if (m_doPerfTest)
			{
				UpdateFOV();
				return;
			}

			grid.Columns = m_visionMap.Width;

			for (int y = -m_visionRange; y <= m_visionRange; ++y)
			{
				for (int x = -m_visionRange; x <= m_visionRange; ++x)
				{
					var label = new Label();
					label.Width = m_tileSize;
					label.Height = m_tileSize;
					label.BorderBrush = Brushes.Black;
					label.BorderThickness = new Thickness(1, 1, 0, 0);
					label.Tag = new IntVector2(x, y);
					label.MouseDown += new MouseButtonEventHandler(label_MouseDown);
					grid.Children.Add(label);
				}
			}

			Dispatcher.BeginInvoke(new Action(UpdateFOV), null);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			var dir = KeyToDir(e.Key);

			if (dir == Direction.None)
			{
				base.OnKeyDown(e);
				return;
			}

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
			var b = (Label)sender;
			var p = (IntVector2)b.Tag;

			p = p.Offset(m_viewerLocation.X, m_viewerLocation.Y);

			m_blockerMap[p] = !m_blockerMap[p];

			UpdateFOV();
		}

		void Calc(IntVector2 viewerLocation, int visionRange, Grid2D<bool> visibilityMap, IntSize2 mapSize,
			Func<IntVector2, bool> blockerDelegate)
		{
			m_algoDel(viewerLocation, visionRange, visibilityMap, mapSize, blockerDelegate);
		}

		void UpdateFOV()
		{
			if (m_doPerfTest)
			{
				Calc(m_viewerLocation, m_visionRange, m_visionMap, m_blockerMap.Bounds.Size, p => m_blockerMap[p]);

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var sw = Stopwatch.StartNew();

				Calc(m_viewerLocation, m_visionRange, m_visionMap, m_blockerMap.Bounds.Size, p => m_blockerMap[p]);

				sw.Stop();
				Trace.TraceInformation("Elapsed {0} ms", sw.ElapsedMilliseconds);

				Application.Current.Shutdown();
				return;
			}

			Calc(m_viewerLocation, m_visionRange, m_visionMap, m_blockerMap.Bounds.Size, p => m_blockerMap[p]);

			canvas.Children.Clear();

			foreach (Label b in grid.Children)
			{
				var p = (IntVector2)b.Tag;

				if (p == new IntVector2())
				{
					b.Background = Brushes.Red;
					continue;
				}

				var ml = p.Offset(m_viewerLocation.X, m_viewerLocation.Y);

				if (m_blockerMap.Bounds.Contains(ml) == false)
				{
					b.Background = Brushes.DarkRed;
					continue;
				}

				var isBlocker = m_blockerMap[ml];
				var isVis = m_visionMap[p];

				if (isVis)
				{
					if (isBlocker)
					{
						b.Background = Brushes.LightGray;
						bool showLine = false;

						if (showLine && p.X >= 0 && p.Y >= 0 && double.IsNaN(grid.ActualWidth) == false)
						{
							var upperLine = CreateLine(new Point(0.0, 0.0), new Point(p.X - 0.5, p.Y + 0.5), Brushes.Yellow);
							canvas.Children.Add(upperLine);

							var lowerLine = CreateLine(new Point(0.0, 0.0), new Point(p.X + 0.5, p.Y - 0.5), Brushes.Blue);
							canvas.Children.Add(lowerLine);
						}
					}
					else
						b.Background = Brushes.LightGreen;
				}
				else
				{
					if (isBlocker)
						b.Background = Brushes.DimGray;
					else
						b.Background = Brushes.DarkGreen;
				}
			}
		}

		Line CreateLine(Point p1, Point p2, Brush brush)
		{
			var ts = m_tileSize;

			var translatex = new Func<double, double>(v => (m_visionRange + v) * ts + 0.5 * ts);
			var translatey = new Func<double, double>(v => (m_visionRange + v) * ts + 0.5 * ts);

			var line = new Line()
			{
				Stroke = brush,
				StrokeThickness = 1,
				X1 = translatex(p1.X),
				Y1 = translatey(p1.Y),
				X2 = translatex((p2.X - p1.X) * 10),
				Y2 = translatey((p2.Y - p1.Y) * 10),
				IsHitTestVisible = false,
			};

			return line;
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

				case LOSAlgo.RayCast:
					m_algoDel = RayCast.Calculate;
					break;

				default:
					m_algoDel = null;
					break;
			}
		}
	}
}
