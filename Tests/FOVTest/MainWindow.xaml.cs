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
	public partial class MainWindow : Window
	{
		ILOSAlgo m_los = new ShadowCastRecursive();
		int m_visionRange = 16;
		Grid2D<bool> m_blockerMap;
		Grid2D<bool> m_visionMap;
		double m_tileSize;

		bool m_doPerfTest = false;

		public MainWindow()
		{
			m_blockerMap = new Grid2D<bool>(m_visionRange * 2 + 1, m_visionRange * 2 + 1);
			m_visionMap = new Grid2D<bool>(m_blockerMap.Width, m_blockerMap.Height);
			m_visionMap.Origin = new IntVector2(m_visionRange, m_visionRange);

			m_blockerMap.Origin = new IntVector2(m_visionRange, m_visionRange);

			//m_blockerMap[2, 1] = true;

			
			m_blockerMap[12, 8] = true;
			m_blockerMap[12, 9] = true;

			m_blockerMap[13, 0] = true;
			m_blockerMap[13, 1] = true;

			m_blockerMap[13, 4] = true;

			m_blockerMap[13, 7] = true;
			m_blockerMap[13, 8] = true;
			m_blockerMap[13, 9] = true;

			m_blockerMap[14, 7] = true;
			m_blockerMap[14, 8] = true;
			m_blockerMap[14, 9] = true;

			m_blockerMap[15, 7] = true;
			m_blockerMap[15, 8] = true;
			m_blockerMap[15, 9] = true;
			
			m_blockerMap.Origin = new IntVector2();

			m_tileSize = 16;

			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			if (m_doPerfTest)
			{
				UpdateFOV();
				return;
			}

			grid.Columns = m_blockerMap.Width;

			for (int y = -m_visionRange; y <= m_visionRange; ++y)
			{
				for (int x = -m_visionRange; x <= m_visionRange; ++x)
				{
					var label = new Label();
					label.Width = m_tileSize;
					label.Height = m_tileSize;
					label.BorderBrush = Brushes.Black;
					label.BorderThickness = new Thickness(1, 1, 0, 0);
					label.Tag = new IntPoint2(x, -y);
					label.MouseDown += new MouseButtonEventHandler(label_MouseDown);
					grid.Children.Add(label);
				}
			}

			UpdateFOV();
		}

		void label_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var b = (Label)sender;
			var p = (IntPoint2)b.Tag;

			p = p + m_visionMap.Origin;

			m_blockerMap[p] = !m_blockerMap[p];

			UpdateFOV();
		}

		void UpdateFOV()
		{
			if (m_doPerfTest)
			{
				m_los.Calculate(new IntPoint2(m_visionRange, m_visionRange), m_visionRange, m_visionMap, new IntSize2(1000, 1000), p => m_blockerMap[p]);

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var sw = Stopwatch.StartNew();

				m_los.Calculate(new IntPoint2(m_visionRange, m_visionRange), m_visionRange, m_visionMap, new IntSize2(1000, 1000), p => m_blockerMap[p]);

				sw.Stop();
				Trace.TraceInformation("Elapsed {0} ms", sw.ElapsedMilliseconds);

				return;
			}

			m_los.Calculate(new IntPoint2(m_visionRange, m_visionRange), m_visionRange, m_visionMap, new IntSize2(1000, 1000), p => m_blockerMap[p]);

			canvas.Children.Clear();

			foreach (Label b in grid.Children)
			{
				var p = (IntPoint2)b.Tag;

				if (p == new IntPoint2())
				{
					b.Background = Brushes.Red;
					continue;
				}

				var isBlocker = m_blockerMap[p + m_visionMap.Origin];
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
			var translatey = new Func<double, double>(v => (m_visionRange - v) * ts + 0.5 * ts);

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
			var cbi = (ComboBoxItem)cb.SelectedItem;
			int id = int.Parse((string)cbi.Tag);

			switch (id)
			{
				case 1:
					m_los = new ShadowCastRecursive();
					break;

				case 2:
					m_los = new LOSShadowCast1();
					break;

				default:
					throw new Exception();
			}

			UpdateFOV();
		}
	}
}
