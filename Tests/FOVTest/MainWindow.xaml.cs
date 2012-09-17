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
		LOSShadowCast1 m_los = new LOSShadowCast1();
		int m_visionRange = 3;
		Grid2D<bool> m_blockers;
		Grid2D<bool> m_vis;

		public MainWindow()
		{
			m_blockers = new Grid2D<bool>(m_visionRange * 2 + 1, m_visionRange * 2 + 1);
			m_vis = new Grid2D<bool>(m_blockers.Width, m_blockers.Height);

			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			var cols = m_blockers.Width;

			grid.Columns = cols;

			for (int y = 0; y < cols; ++y)
			{
				for (int x = 0; x < cols; ++x)
				{
					var label = new Label();
					label.BorderBrush = Brushes.Green;
					label.BorderThickness = new Thickness(1);
					label.Tag = new IntPoint2(x, y);
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

			m_blockers[p] = !m_blockers[p];

			UpdateFOV();
		}

		void UpdateFOV()
		{
			m_vis.Origin = new IntVector2(m_visionRange, m_visionRange);
			m_los.Calculate(new IntPoint2(m_visionRange, m_visionRange), m_visionRange, m_vis, new IntSize2(1000, 1000), p => m_blockers[p]);

			foreach (Label b in grid.Children)
			{
				var p = (IntPoint2)b.Tag;

				var isBlocker = m_blockers[p];
				var isVisible = m_vis[p - m_vis.Origin];

				if (isVisible)
				{
					if (isBlocker)
						b.Background = Brushes.LightGray;
					else
						b.Background = Brushes.Black;
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
	}
}
