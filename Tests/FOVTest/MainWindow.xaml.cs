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

namespace FOVTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		NewFov m_los = new NewFov();
		int m_visionRange = 16;
		Grid2D<bool> m_blockers;
		Grid2D<FovData> m_vis;

		public MainWindow()
		{
			m_blockers = new Grid2D<bool>(m_visionRange * 2 + 1, m_visionRange * 2 + 1);
			m_vis = new Grid2D<FovData>(m_blockers.Width, m_blockers.Height);
			m_vis.Origin = new IntVector2(m_visionRange, m_visionRange);

			m_blockers.Origin = new IntVector2(m_visionRange, m_visionRange);

			m_blockers[12, 8] = true;
			m_blockers[12, 9] = true;

			m_blockers[13, 0] = true;
			m_blockers[13, 1] = true;
			m_blockers[13, 4] = true;
			m_blockers[13, 7] = true;
			m_blockers[13, 8] = true;
			m_blockers[13, 9] = true;

			m_blockers[14, 7] = true;
			m_blockers[14, 8] = true;
			m_blockers[14, 9] = true;

			m_blockers[15, 7] = true;
			m_blockers[15, 8] = true;
			m_blockers[15, 9] = true;

			m_blockers.Origin = new IntVector2();

			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			grid.Columns = m_blockers.Width;

			for (int y = -m_visionRange; y <= m_visionRange; ++y)
			{
				for (int x = -m_visionRange; x <= m_visionRange; ++x)
				{
					var label = new Label();
					label.BorderBrush = Brushes.Green;
					label.BorderThickness = new Thickness(1);
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

			p = p + m_vis.Origin;

			m_blockers[p] = !m_blockers[p];

			UpdateFOV();
		}

		void UpdateFOV()
		{
			m_los.Calculate(new IntPoint2(m_visionRange, m_visionRange), m_visionRange, m_vis, new IntSize2(1000, 1000), p => m_blockers[p]);

			canvas.Children.Clear();

			foreach (Label b in grid.Children)
			{
				var p = (IntPoint2)b.Tag;

				var isBlocker = m_blockers[p + m_vis.Origin];
				var fovData = m_vis[p];

				b.Content = fovData.id.ToString();

				if (fovData.vis)
				{
					if (isBlocker)
					{
						b.Background = Brushes.LightGray;
						bool showLine = true;

						if (showLine)
						{

							var w = grid.Width / (m_visionRange * 2 + 1);

							var translatex = new Func<double, double>(v => (m_visionRange + v) * w + 0.5 * w);
							var translatey = new Func<double, double>(v => (m_visionRange - v) * w + 0.5 * w);

							var upperLine = CreateLine(new Point(0.0, 0.0), new Point(p.X - 0.5, p.Y + 0.5), Brushes.Yellow);
							canvas.Children.Add(upperLine);

							var lowerLine = CreateLine(new Point(0.0, 0.0), new Point(p.X + 0.5, p.Y - 0.5), Brushes.Blue);
							canvas.Children.Add(lowerLine);
						}
					}
					else
						b.Background = Brushes.DarkGray;
				}
				else
				{
					if (isBlocker)
						b.Background = Brushes.DarkRed;
					else
						b.Background = Brushes.Red;
				}
			}
		}

		Line CreateLine(Point p1, Point p2, Brush brush)
		{
			var w = grid.Width / (m_visionRange * 2 + 1);

			var translatex = new Func<double, double>(v => (m_visionRange + v) * w + 0.5 * w);
			var translatey = new Func<double, double>(v => (m_visionRange - v) * w + 0.5 * w);

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
	}
}
